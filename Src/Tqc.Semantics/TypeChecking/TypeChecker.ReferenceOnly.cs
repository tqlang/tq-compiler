using Tqc.Core;
using Tqc.IR;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal sealed partial class TypeChecker
{
    /// <summary>
    /// Semantic rule: a class (a struct marked `@class`, a .NET class or interface, or what extends one) only exists by reference,
    /// so it is never read outside a `Ref` node. `List(i32) items` is an error, `*List(i32) items` is not.
    /// Looks through `[]T`, `[n]T`, `?T`, `!T` and the arguments of generic types; `*T` is what is asked for.
    /// </summary>
    private void CheckValueType(IrExpression? written)
    {
        switch (written)
        {
            case IrSliceTypeModifier slice:
                CheckValueType(slice.subtype);
                return;

            case IrArrayTypeModifier array:
                CheckValueType(array.subtype);
                return;

            case IrReference { Reference: TypeMetaReference { Type: IStruct structure } } reference:
                CheckStructValue(structure, reference.Location, written: true);
                return;
        }
    }

    private void CheckStructValue(IStruct structure, SourceLocation location, bool written)
    {
        if (structure is GenericInstance instance)
        {
            // `*T`: the reference is there; what is inside may still have class arguments (`*List(Foo)`).
            if (ReferenceEquals(instance.Definition, CompilerStd.Types.Ref))
            {
                if (instance.Arguments is [IrReference { Reference: TypeMetaReference { Type: GenericInstance inner } }])
                    CheckArguments(inner);
                else if (instance.Arguments is [var operand] && operand is not IrReference) CheckValueType(operand);
                return;
            }

            if (StructSemantics.IsReference(structure))
            {
                ReportByValue(structure, location);
                return;
            }

            CheckArguments(instance);
            return;
        }

        if (structure.Parameters.Count == 0 && StructSemantics.IsReference(structure)) ReportByValue(structure, location);
    }

    private void CheckArguments(GenericInstance instance)
    {
        foreach (var argument in instance.Arguments) CheckValueType(argument);
    }

    private void ReportByValue(IStruct structure, SourceLocation location)
    {
        var name = TypeFormatter.Format(new StructTypeReference(structure));
        Error($"'{name}' is a class and cannot be used as a value: it only exists behind a reference; use '*{name}'", location);
    }
}
