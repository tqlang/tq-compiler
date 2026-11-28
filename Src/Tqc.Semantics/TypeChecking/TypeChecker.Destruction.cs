using Tqc.Core;
using Tqc.IR;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// Where a variable that has a destructor stops being accessible, the destructor is called: at the end of its block, before a `return` or a
/// `break` that leaves it, and when a new value replaces the one it has. The type checker adds the <see cref="IrDestroy"/> there, once the
/// function is checked; a variable that `delete` already destroyed is skipped when the code runs (the backend keeps a flag for those).
/// A parameter by value is the function's own copy, so it is destroyed too; one that is a reference (`*T`) is not, it belongs to the caller.
/// </summary>
internal sealed partial class TypeChecker
{
    private TypeTable TypeTable => types;

    private sealed class Owned(object key, IrExpression variable)
    {
        public object Key { get; } = key;
        public IrExpression Variable { get; } = variable;
    }

    private sealed class DestructionScope(bool isLoop)
    {
        public bool IsLoop { get; } = isLoop;
        public List<Owned> Variables { get; } = [];
    }

    private sealed class Destruction(TypeChecker checker)
    {
        private readonly List<DestructionScope> _scopes = [];

        public void Function(TqFunction function)
        {
            var parameters = new List<Owned>();
            foreach (var parameter in function.Parameters ?? [])
            {
                if (parameter.Type == null) continue;
                var type = TypeFromExpression(parameter.Type);
                if (type == null || CompilerStd.Types.IsRef(type, out _) || !Destructors.Has(type)) continue;

                var reference = new IrReference(parameter.Type.location, new ParameterReference(parameter));
                checker.TypeTable.Set(reference, type);
                parameters.Add(new Owned(parameter, reference));
            }

            Body(function.Body!, isLoop: false, parameters);
        }

        private void Body(IrBody body, bool isLoop, IEnumerable<Owned>? first = null)
        {
            var scope = new DestructionScope(isLoop);
            if (first != null) scope.Variables.AddRange(first);
            _scopes.Add(scope);

            for (var index = 0; index < body.Children.Count; index++) body.Children[index] = Node(body.Children[index]);

            _scopes.RemoveAt(_scopes.Count - 1);
            if (body.Children.Count == 0 || body.Children[^1] is not (IrReturnStatement or IrBreak))
                body.Children.AddRange(Destroys(scope.Variables));
        }

        private static IEnumerable<IrStatement> Destroys(IEnumerable<Owned> variables) =>
            variables.Reverse().Select(owned => (IrStatement)new IrDestroy(owned.Variable.Location, owned.Variable));

        /// <summary>A statement that is not a block: a scope of its own, as the block it would be.</summary>
        private IrStatement Branch(IrStatement statement, bool isLoop)
        {
            if (statement is IrBody body)
            {
                Body(body, isLoop);
                return body;
            }

            var scope = new DestructionScope(isLoop);
            _scopes.Add(scope);
            var rewritten = (IrStatement)Node(statement);
            _scopes.RemoveAt(_scopes.Count - 1);
            if (scope.Variables.Count == 0 && !isLoop) return rewritten;

            var block = new IrBody(statement.Location);
            block.Children.Add(rewritten);
            if (rewritten is not (IrReturnStatement or IrBreak)) block.Children.AddRange(Destroys(scope.Variables));
            return block;
        }

        private IrNode Node(IrNode node)
        {
            switch (node)
            {
                case IrBody body:
                    Body(body, isLoop: false);
                    return body;

                case IrIf ifStatement:
                    return ifStatement with
                    {
                        Then = Branch(ifStatement.Then, false),
                        Else = ifStatement.Else == null ? null : Branch(ifStatement.Else, false),
                    };

                case IrWhile loop:
                    return loop with { Body = Branch(loop.Body, true) };

                case IrReturnStatement returnStatement:
                {
                    var cleanup = Destroys(_scopes.SelectMany(scope => scope.Variables).ToList()).ToArray();
                    return cleanup.Length == 0 ? returnStatement : returnStatement with { Cleanup = cleanup };
                }

                case IrBreak @break:
                {
                    // The scopes the loop leaves: the ones inside it, down to its own.
                    var leaving = new List<Owned>();
                    for (var index = _scopes.Count - 1; index >= 0; index--)
                    {
                        leaving.InsertRange(0, _scopes[index].Variables);
                        if (_scopes[index].IsLoop) break;
                    }

                    var cleanup = Destroys(leaving).ToArray();
                    return cleanup.Length == 0 ? @break : @break with { Cleanup = cleanup };
                }

                case IrSizeChoiceStatement choice:
                    return choice with
                    {
                        Variants = choice.Variants.Select(variant => variant with { Statement = (IrStatement)Node(variant.Statement) }).ToArray(),
                    };

                case IrExpressionStatement statement:
                    return statement with { Expression = (IrExpression)Node(statement.Expression) };

                case IrLocal local:
                    Declare(local);
                    return local;

                case IrAssign { Target: IrLocal local } assign:
                    Declare(local);
                    return assign;

                case IrAssign assign when LifetimeChecker.VariableKey(assign.Target) is { } key && IsOwned(key):
                    return assign with { DestroyOld = true };

                case IrMatch match:
                    return match with
                    {
                        Arms = match.Arms.Select(arm => arm.Body is IrBody body ? arm with { Body = Branch(body, false) } : arm).ToArray(),
                    };

                default:
                    return node;
            }
        }

        private void Declare(IrLocal local)
        {
            if (checker.TypeTable.Get(local) is not { } type || !Destructors.Has(type)) return;

            var reference = new IrReference(local.Location, new LocalReference(local));
            checker.TypeTable.Set(reference, type);
            _scopes[^1].Variables.Add(new Owned(LifetimeChecker.VariableKey(local)!, reference));
        }

        private bool IsOwned(object key) => _scopes.Any(scope => scope.Variables.Any(owned => owned.Key.Equals(key)));
    }
}
