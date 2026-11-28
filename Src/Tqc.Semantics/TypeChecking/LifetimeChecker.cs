using System.Reflection;
using Tqc.Core;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// Follows the variables of a function that `delete` has destroyed, along every path the code can take, and reports a variable that is
/// used, or deleted again, after that. A variable is alive again when it is assigned (or declared again, in the next turn of a loop).
/// It knows two things about a deleted variable: that it was deleted on every path that reaches a place, or only on some of them.
/// </summary>
internal sealed class LifetimeChecker(Action<string, SourceLocation> report)
{
    /// <summary>The variables that are deleted at a place: true when on every path that gets there, false when only on some.</summary>
    private sealed class State : Dictionary<object, bool>
    {
        public State() { }
        public State(State copy) : base(copy) { }

        public bool SameAs(State other) => Count == other.Count && this.All(pair => other.TryGetValue(pair.Key, out var value) && value == pair.Value);
    }

    /// <summary>The variables a `delete` destroys (the keys of <see cref="VariableKey"/>).</summary>
    public HashSet<object> Deleted { get; } = [];

    private readonly HashSet<(string, SourceLocation)> _reported = [];
    private readonly Stack<List<State>> _breaks = new();
    private readonly Dictionary<object, string> _names = [];

    public void Check(TqFunction function)
    {
        if (function.Body == null) return;

        State? state = new();
        VisitNode(function.Body, ref state);
    }

    /// <summary>Where two paths meet: deleted on both is deleted; on one only, maybe. A place only one path reaches is that path's.</summary>
    private static State? Join(State? left, State? right)
    {
        if (left == null) return right;
        if (right == null) return left;

        var joined = new State();
        foreach (var (key, definite) in left) joined[key] = definite && right.TryGetValue(key, out var other) && other;
        foreach (var (key, _) in right) joined.TryAdd(key, false);
        return joined;
    }

    private static State? Copy(State? state) => state == null ? null : new State(state);

    /// <summary>What identifies a variable: a local by its name and place, a parameter by itself. Null for what is not one.</summary>
    public static object? VariableKey(IrNode node) => KeyOf(node, out _);

    private static object? KeyOf(IrNode node, out string name)
    {
        switch (node)
        {
            case IrReference { Reference: LocalReference local }:
                name = local.Local.name;
                return (local.Local.name, local.Local.Location);
            case IrReference { Reference: ParameterReference parameter }:
                name = parameter.Parameter.Name;
                return parameter.Parameter;
            case IrLocal declared:
                name = declared.name;
                return (declared.name, declared.Location);
            default:
                name = "";
                return null;
        }
    }

    private void Report(string message, SourceLocation location)
    {
        if (_reported.Add((message, location))) report(message, location);
    }

    private void VisitNode(IrNode node, ref State? state)
    {
        if (state == null) return;

        switch (node)
        {
            case IrBody body:
                foreach (var child in body.Children)
                {
                    VisitNode(child, ref state);
                    if (state == null) return;
                }
                return;

            case IrReturnStatement returnStatement:
                if (returnStatement.ReturnValue != null) VisitNode(returnStatement.ReturnValue, ref state);
                state = null;
                return;

            case IrBreak:
                if (_breaks.Count > 0) _breaks.Peek().Add(Copy(state)!);
                state = null;
                return;

            case IrDelete @delete:
            {
                var key = KeyOf(@delete.Operand, out var name);
                if (key != null && state.TryGetValue(key, out var definite))
                {
                    Report(definite ? $"'{name}' is already deleted" : $"'{name}' may be already deleted", @delete.Operand.Location);
                    return;
                }

                VisitNode(@delete.Operand, ref state);
                if (key != null) Deleted.Add(key);
                if (key != null && state != null) state[key] = true;
                return;
            }

            case IrIf ifStatement:
            {
                VisitNode(ifStatement.Condition, ref state);
                var otherwise = Copy(state);
                if (ifStatement.Else != null) VisitNode(ifStatement.Else, ref otherwise);
                VisitNode(ifStatement.Then, ref state);
                state = Join(state, otherwise);
                return;
            }

            case IrWhile loop:
                VisitLoop(loop, ref state);
                return;

            case IrAssign assign:
            {
                VisitNode(assign.Source, ref state);
                if (state == null) return;

                // What is assigned to a variable is a new value: the variable is alive.
                if (KeyOf(assign.Target, out _) is { } key) state.Remove(key);
                else VisitNode(assign.Target, ref state);
                return;
            }

            case IrLocal declared:
                state.Remove((declared.name, declared.Location));
                return;

            case IrMatch match:
            {
                VisitNode(match.Subject, ref state);
                if (state == null) return;

                State? joined = match.Arms.Any(arm => arm.IsDefault) ? null : Copy(state);
                foreach (var arm in match.Arms)
                {
                    var armState = Copy(state);
                    VisitNode(arm.Body, ref armState);
                    joined = Join(joined, armState);
                }

                state = joined;
                return;
            }

            case IrSizeChoice choice:
                VisitAlternatives(choice.Variants.Select(variant => (IrNode)variant.Expression), ref state);
                return;

            case IrSizeChoiceStatement choice:
                VisitAlternatives(choice.Variants.Select(variant => (IrNode)variant.Statement), ref state);
                return;

            case IrReference reference:
                if (KeyOf(node, out var used) is { } usedKey && state.TryGetValue(usedKey, out var deleted))
                    Report(deleted ? $"'{used}' was deleted and cannot be used" : $"'{used}' may have been deleted and cannot be used", reference.Location);
                return;

            default:
                foreach (var child in ChildrenOf(node))
                {
                    VisitNode(child, ref state);
                    if (state == null) return;
                }
                return;
        }
    }

    private void VisitAlternatives(IEnumerable<IrNode> alternatives, ref State? state)
    {
        State? joined = null;
        foreach (var alternative in alternatives)
        {
            var copy = Copy(state);
            VisitNode(alternative, ref copy);
            joined = Join(joined, copy);
        }

        state = joined;
    }

    /// <summary>
    /// A loop is visited until what it deletes stops growing: the state at its start is where it comes from joined with where each
    /// turn ends, because a variable deleted in one turn is dead in the next. Errors are reported once.
    /// </summary>
    private void VisitLoop(IrWhile loop, ref State? state)
    {
        if (loop.Declaration != null) VisitNode(loop.Declaration, ref state);
        if (state == null) return;

        var head = state;
        State? exit = null;

        for (var turn = 0; turn < 16; turn++)
        {
            var current = Copy(head);
            _breaks.Push([]);

            VisitNode(loop.Condition, ref current);
            exit = Copy(current);
            VisitNode(loop.Body, ref current);
            if (loop.Step != null) VisitNode(loop.Step, ref current);

            var breaks = _breaks.Pop();
            foreach (var @break in breaks) exit = Join(exit, @break);

            var next = Join(head, current)!;
            if (next.SameAs(head)) break;
            head = next;
        }

        state = exit;
    }

    private static readonly Dictionary<Type, PropertyInfo[]> _properties = [];

    /// <summary>The nodes a node is made of, in the order it declares them (what is evaluated first is declared first).</summary>
    internal static IEnumerable<IrNode> ChildrenOf(IrNode node)
    {
        var type = node.GetType();
        PropertyInfo[] properties;
        lock (_properties)
        {
            if (!_properties.TryGetValue(type, out properties!))
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.Name != "EqualityContract" && property.GetIndexParameters().Length == 0
                                       && (typeof(IrNode).IsAssignableFrom(property.PropertyType)
                                           || (property.PropertyType.IsArray && typeof(IrNode).IsAssignableFrom(property.PropertyType.GetElementType()))))
                    .ToArray();
                _properties[type] = properties;
            }
        }

        foreach (var property in properties)
        {
            switch (property.GetValue(node))
            {
                case IrNode child: yield return child; break;
                case IrNode[] children:
                    foreach (var element in children) yield return element;
                    break;
            }
        }
    }
}
