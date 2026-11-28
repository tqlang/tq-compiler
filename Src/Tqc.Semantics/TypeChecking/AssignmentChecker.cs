using System.Reflection;
using Tqc.Core;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// A variable declared without a value (`let T x`) cannot be used before one is assigned to it, on every path that gets to
/// where it is used. A fixed array is not one of these: the declaration makes its memory, with every element zeroed.
/// </summary>
/// <remarks>
/// The variable takes a value as a whole: writing a field of it uses it. Passing `&amp;x` to an `out` parameter of .NET
/// assigns it. Conditions are not evaluated.
/// </remarks>
internal sealed class AssignmentChecker(Action<string, SourceLocation> report, Func<IrExpression, ITypeRef?> typeOf)
{
    /// <summary>The variables that may have no value at a place (the keys of <see cref="KeyOf"/>).</summary>
    private sealed class State : HashSet<(string, SourceLocation)>
    {
        public State() { }
        public State(State copy) : base(copy) { }
    }

    /// <summary>For each loop around the place, what its `break`s leave without a value.</summary>
    private readonly Stack<List<State>> _breaks = new();

    private readonly HashSet<(string, SourceLocation)> _reported = [];

    public void Check(IrBody body)
    {
        // The state is null where the code cannot get: nothing is used there.
        State? state = new();
        Visit(body, ref state);
    }

    private static (string, SourceLocation) KeyOf(IrLocal local) => (local.name, local.Location);

    /// <summary>Where paths meet, a variable that has no value coming from one of them may have none.</summary>
    private static State? Join(State? left, State? right)
    {
        if (left == null) return right;
        if (right == null) return left;

        var joined = new State(left);
        joined.UnionWith(right);
        return joined;
    }

    private static State? Copy(State? state) => state == null ? null : new State(state);

    private void Visit(IrNode node, ref State? state)
    {
        if (state == null) return;

        switch (node)
        {
            case IrLocal declared:
                if (declared.type == null || TypeExpressions.Raw(declared.type) is not ArrayTypeRef) state.Add(KeyOf(declared));
                break;

            case IrAssign { Target: IrLocal declared } assign:
                Visit(assign.Source, ref state);
                state?.Remove(KeyOf(declared));
                break;

            case IrAssign { Target: IrReference { Reference: LocalReference local } } assign:
                Visit(assign.Source, ref state);
                state?.Remove(KeyOf(local.Local));
                break;

            case IrReference { Reference: LocalReference local } reference:
                if (state.Contains(KeyOf(local.Local)) && _reported.Add((local.Local.name, reference.Location)))
                    report($"'{local.Local.name}' is used before a value is assigned to it", reference.Location);
                break;

            case IrCall call:
                VisitCall(call, ref state);
                break;

            case IrReturnStatement returnStatement:
                if (returnStatement.ReturnValue != null) Visit(returnStatement.ReturnValue, ref state);
                state = null;
                return;

            case IrBreak:
                if (_breaks.Count > 0) _breaks.Peek().Add(Copy(state)!);
                state = null;
                return;

            case IrIf ifStatement:
            {
                Visit(ifStatement.Condition, ref state);
                var otherwise = Copy(state);
                if (ifStatement.Else != null) Visit(ifStatement.Else, ref otherwise);
                Visit(ifStatement.Then, ref state);
                state = Join(state, otherwise);
                return;
            }

            case IrWhile loop:
                VisitLoop(loop, ref state);
                return;

            // The right side runs only when the left one has nothing: what it assigns may not be.
            case IrBinary { Operator: BinaryOperator.Coalesce } coalesce:
            {
                Visit(coalesce.Left, ref state);
                var right = Copy(state);
                Visit(coalesce.Right, ref right);
                break;
            }

            // The arms cover every value (see VisitMatch), so one of them runs.
            case IrMatch match:
            {
                Visit(match.Subject, ref state);
                if (state == null) return;

                State? joined = match.Arms.Length == 0 ? Copy(state) : null;
                foreach (var arm in match.Arms)
                {
                    var armState = Copy(state);
                    foreach (var pattern in arm.Conditions ?? arm.Patterns) Visit(pattern, ref armState);
                    Visit(arm.Body, ref armState);
                    joined = Join(joined, armState);
                }

                state = joined;
                return;
            }

            case IrSizeChoice choice:
                VisitAlternatives(choice.Variants.Select(variant => (IrNode)variant.Expression), ref state);
                break;

            case IrSizeChoiceStatement choice:
                VisitAlternatives(choice.Variants.Select(variant => (IrNode)variant.Statement), ref state);
                return;

            case IrBody body:
                foreach (var child in body.Children) Visit(child, ref state);
                return;

            default:
                foreach (var child in ChildrenOf(node)) Visit(child, ref state);
                break;
        }

        // Nothing runs after a call of a `noreturn` function.
        if (node is IrExpression expression && typeOf(expression) is { } type
            && ConversionRules.IsStruct(type, CompilerStd.Types.NoReturn))
            state = null;
    }

    private void VisitCall(IrCall call, ref State? state)
    {
        var reference = call.callee switch
        {
            IrReference named => named.Reference,
            IrAccess { access.Reference: var member } => member,
            _ => null,
        };
        var method = reference is FunctionTypeReference { Function: DotnetMethod dotnet } ? dotnet : null;

        Visit(call.callee, ref state);

        var arguments = call.ArgumentsList.arguments;
        var types = method?.TypeParameters.Count ?? 0;
        // An extension method called through a value takes it as its first parameter, which is not among the arguments.
        var receiver = method != null && method.ParameterTypes.Count > arguments.Length - types ? 1 : 0;

        // What goes to `out` has its value when the function returns, after every argument was evaluated.
        List<(string, SourceLocation)> written = [];
        for (var i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] is IrAddressOf { Operand: IrReference { Reference: LocalReference local } }
                && method != null && i >= types && method.IsOut(i - types + receiver))
                written.Add(KeyOf(local.Local));
            else
                Visit(arguments[i], ref state);
        }

        foreach (var variable in written) state?.Remove(variable);
    }

    private void VisitAlternatives(IEnumerable<IrNode> alternatives, ref State? state)
    {
        State? joined = null;
        foreach (var alternative in alternatives)
        {
            var copy = Copy(state);
            Visit(alternative, ref copy);
            joined = Join(joined, copy);
        }

        state = joined;
    }

    /// <summary>
    /// One turn is enough: no turn takes the value of a variable declared outside the loop, and one declared inside it is
    /// declared again in the next.
    /// </summary>
    private void VisitLoop(IrWhile loop, ref State? state)
    {
        if (loop.Declaration != null) Visit(loop.Declaration, ref state);
        if (state == null) return;

        _breaks.Push([]);
        Visit(loop.Condition, ref state);

        // `while true` only ends at a `break`.
        var exit = loop.Condition is IrComptimeBool { Value: true } ? null : Copy(state);

        Visit(loop.Body, ref state);
        if (loop.Step != null) Visit(loop.Step, ref state);

        foreach (var @break in _breaks.Pop()) exit = Join(exit, @break);
        state = exit;
    }

    private static readonly Dictionary<Type, PropertyInfo[]> Properties = [];

    /// <summary>The nodes a node is made of, in the order it declares them (what is evaluated first is declared first).</summary>
    private static IEnumerable<IrNode> ChildrenOf(IrNode node)
    {
        PropertyInfo[] properties;
        lock (Properties)
        {
            if (!Properties.TryGetValue(node.GetType(), out properties!))
            {
                properties = node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.GetIndexParameters().Length == 0
                        && (typeof(IrNode).IsAssignableFrom(property.PropertyType)
                            || typeof(IrNode).IsAssignableFrom(property.PropertyType.GetElementType())))
                    .ToArray();
                Properties[node.GetType()] = properties;
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
