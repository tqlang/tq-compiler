using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal sealed partial class TypeChecker
{
    /// <summary>
    /// The type a type expression names (`i32`, `Foo`, `[]string`); null if it is not known here.
    /// A generic instance, such as `Integer(true, 32)`, is an entry of the instance table and counts as a struct.
    /// </summary>
    internal static ITypeRef? TypeFromExpression(IrExpression expression)
    {
        switch (expression)
        {
            // Only a struct without parameters is a type; a generic one is a type constructor and needs arguments (see GenericInstance).
            case IrReference { Reference: TypeMetaReference { Type: IStruct { Parameters.Count: 0 } structure } }:
                return StructSemantics.TypeOf(structure);

            case IrReference { Reference: TypeMetaReference { Type: TqTypedef typedef } }:
                return new TypedefTypeReference(typedef);

            case IrReference { Reference: ProtocolTypeReference protocol }:
                return protocol;

            // `T` of `func f(type T, T value)`.
            case IrReference when GenericParameterTypeRef.Of(expression) is { } generic:
                return generic;

            case IrSliceTypeModifier slice:
                var element = TypeFromExpression(slice.subtype);
                return element != null ? new SliceTypeRef(element) : null;

            case IrArrayTypeModifier { Length: IrComptimeInt { BigInteger: var length } } array when length >= 0 && length <= int.MaxValue:
                var arrayElement = TypeFromExpression(array.subtype);
                return arrayElement != null ? new ArrayTypeRef((int)length, arrayElement) : null;

            default:
                return null;
        }
    }
}
