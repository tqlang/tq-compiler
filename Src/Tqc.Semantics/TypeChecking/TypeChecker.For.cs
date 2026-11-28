using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal sealed partial class TypeChecker
{
    /// <summary>
    /// `for` iterates arrays, slices and what is `IEnumerable`: the item has the type of the elements and the index is an
    /// `int`. How it is iterated is up to the backend.
    /// </summary>
    private IrStatement CheckFor(IrFor loop)
    {
        var collection = VisitPerSize(loop.Collection);
        var type = TypeOf(collection);

        if ((ElementOf(type) ?? (type != null ? EnumeratedElement(type) : null)) is { } element) types.Set(loop.Item, element);
        else if (type != null)
            Error($"'for' iterates an array, a slice or an 'IEnumerable', not '{TypeFormatter.Format(type)}'", collection.Location);

        if (loop.Index != null) types.Set(loop.Index, new StructTypeReference(CompilerStd.Types.Natural(true)));

        _loopDepth++;
        var body = (IrStatement)CheckNode(loop.Body);
        _loopDepth--;

        return loop with { Collection = collection, Body = body };
    }

    /// <summary>
    /// The `T` of the `IEnumerable(T)` the type is or implements; `object` if it is only the `IEnumerable` without a type;
    /// null if it is neither.
    /// </summary>
    private static ITypeRef? EnumeratedElement(ITypeRef type)
    {
        ITypeRef? untyped = null;
        var start = ConversionRules.Unwrap(type);
        HashSet<ITypeRef> visited = [start];
        Queue<ITypeRef> pending = new([start]);

        // Nearest first. A generic type that extends itself with other arguments would never end.
        while (pending.TryDequeue(out var current) && visited.Count < 256)
        {
            switch (ConversionRules.DefinitionOf(current))
            {
                case IDotnetType { TypeReference.FullName: "System.Collections.Generic.IEnumerable`1" }
                    when current is StructTypeReference { Struct: GenericInstance { Arguments: [var argument] } }:
                    return TypeFromExpression(argument);

                case DotnetInterface { TypeReference.FullName: "System.Collections.IEnumerable" } enumerable:
                    untyped = enumerable.ObjectType;
                    break;
            }

            foreach (var parent in EnumerationParents(current))
                if (visited.Add(parent)) pending.Enqueue(parent);
        }

        return untyped;
    }

    /// <summary>What a type extends and implements, of Tq or of .NET, with the arguments of the type in place of its parameters.</summary>
    private static IEnumerable<ITypeRef> EnumerationParents(ITypeRef type)
    {
        IEnumerable<ITypeRef?> declared;
        switch (ConversionRules.DefinitionOf(type))
        {
            case TqStruct structure:
                IEnumerable<IrExpression> written = structure.Extends != null
                    ? [structure.Extends, ..structure.Implements]
                    : structure.Implements;
                declared = written.Select(TypeFromExpression);
                break;

            case DotnetStruct dotnet:
                declared = dotnet.Implements;
                break;

            default:
                return ConversionRules.ParentsOf(type);
        }

        var bound = BindingsOf(type);
        return declared
            .Select(parent => parent != null ? Substitute(parent, bound) : null)
            .OfType<ITypeRef>()
            .Select(ConversionRules.Unwrap);
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
