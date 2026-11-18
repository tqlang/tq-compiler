using Tqc.Core;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.IR.References;

/// <summary>Conversions between a type (ITypeRef) and the bound expression that names it.</summary>
public static class TypeExpressions
{
    /// <summary>A type as an expression, to be used as the argument of a generic struct; null if it cannot be written.</summary>
    public static IrExpression? Of(ITypeRef type, SourceLocation location) => type switch
    {
        StructTypeReference structure => new IrReference(location, new TypeMetaReference(structure.Struct)),
        TypedefTypeReference typedef => new IrReference(location, new TypeMetaReference(typedef.Typedef)),
        ProtocolTypeReference protocol => new IrReference(location, protocol),
        GenericParameterTypeRef generic => new IrReference(location, new ParameterReference(generic.Parameter)),
        SliceTypeRef slice => Of(slice.Element, location) is { } element ? new IrSliceTypeModifier(location, element) : null,
        _ => null,
    };

    /// <summary>
    /// The type a bound expression names, exactly as written: `Foo` is `Foo` also when Foo is by reference
    /// (the type checker wraps those in `Ref(Foo)` when a value of it is meant).
    /// </summary>
    public static ITypeRef? Raw(IrExpression expression) => expression switch
    {
        IrReference { Reference: TypeMetaReference { Type: IStruct structure } } => new StructTypeReference(structure),
        IrReference { Reference: TypeMetaReference { Type: TqTypedef typedef } } => new TypedefTypeReference(typedef),
        IrReference { Reference: ProtocolTypeReference protocol } => protocol,
        IrReference when GenericParameterTypeRef.Of(expression) is { } generic => generic,
        IrSliceTypeModifier slice => Raw(slice.subtype) is { } element ? new SliceTypeRef(element) : null,
        _ => null,
    };
}
