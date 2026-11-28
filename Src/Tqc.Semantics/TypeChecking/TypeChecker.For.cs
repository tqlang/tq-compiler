using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal sealed partial class TypeChecker
{
    /// <summary>`for` iterates arrays and slices: the item has the type of the elements and the index is an `int`.</summary>
    private IrStatement CheckFor(IrFor loop)
    {
        var collection = VisitPerSize(loop.Collection);
        var type = TypeOf(collection);

        if (ElementOf(type) is { } element) types.Set(loop.Item, element);
        else if (type != null)
            Error($"'for' iterates an array or a slice, not '{TypeFormatter.Format(type)}'", collection.Location);

        if (loop.Index != null) types.Set(loop.Index, new StructTypeReference(CompilerStd.Types.Natural(true)));

        _loopDepth++;
        var body = (IrStatement)CheckNode(loop.Body);
        _loopDepth--;

        return loop with { Collection = collection, Body = body };
    }

    /// <summary>
    /// The name of the read-only variable whose value writing to `target` would change, if there is one: the variable
    /// itself, or something inside it. What is reached through a reference or a slice is not inside it.
    /// </summary>
    private string? ReadOnlyVariableHolding(IrExpression target)
    {
        switch (target)
        {
            case IrReference { Reference: LocalReference { Local: { IsReadOnly: true } local } }:
                return local.name;
            case IrAccess access:
                return IsHeldByValue(access.baseExpr) ? ReadOnlyVariableHolding(access.baseExpr) : null;
            case IrIndex index:
                return IsHeldByValue(index.Target) ? ReadOnlyVariableHolding(index.Target) : null;
            case IrSizeChoice choice:
                return ReadOnlyVariableHolding(choice.Variants[0].Expression);
            default:
                return null;
        }
    }

    private bool IsHeldByValue(IrExpression expression) =>
        TypeOf(expression) is { } type and not SliceTypeRef && Destructors.IsByValue(type);

    private void CheckNotReadOnly(IrExpression target, string action)
    {
        if (ReadOnlyVariableHolding(target) is { } name)
            Error($"'{name}' is a variable of a 'for', which is read-only: it cannot be {action}", target.Location);
    }
}
