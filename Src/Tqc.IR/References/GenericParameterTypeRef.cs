using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.IR.References;

/// <summary>
/// `T` of `func f(type T, []T values)`: the type that the parameter `T` of the function stands for. It is only known
/// when the function is called; each parameter is its own type, whatever it is called.
/// </summary>
public sealed record GenericParameterTypeRef(TqParameter Parameter) : Ref, ITypeRef
{
    /// <summary>A parameter of the type `type` is a type parameter: the types the function works with are given first.</summary>
    public static bool IsTypeParameter(TqParameter parameter) =>
        parameter.Type is IrReference { Reference: TypeMetaReference { Type: var type } }
        && ReferenceEquals(type, CompilerStd.Types.Type);

    /// <summary>The type a use of a type parameter names (`T`); null if the expression is not one.</summary>
    public static GenericParameterTypeRef? Of(IrExpression expression) =>
        expression is IrReference { Reference: ParameterReference { Parameter: var parameter } } && IsTypeParameter(parameter)
            ? new GenericParameterTypeRef(parameter)
            : null;

    public bool Equals(GenericParameterTypeRef? other) => other != null && ReferenceEquals(Parameter, other.Parameter);
    public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Parameter);
    public override string ToString() => Parameter.Name;
}
