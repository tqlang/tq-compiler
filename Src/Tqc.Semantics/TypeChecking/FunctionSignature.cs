using Tqc.Dotnet.ModuleBuilder;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// The types of a function, whatever it is written in: what it takes and what it returns.
/// A type that is not known (a generic one, `!T`, a .NET type with no equivalent) is null and matches anything,
/// so that comparing signatures never reports a difference that is not certain.
/// </summary>
internal sealed class FunctionSignature(IReadOnlyList<ITypeRef?> parameters, ITypeRef? returnType)
{
    public IReadOnlyList<ITypeRef?> Parameters => parameters;

    /// <summary>What it returns; `void` for a function that returns nothing. Null if it is not known.</summary>
    public ITypeRef? Return => returnType;

    public static FunctionSignature? Of(IFunction function) => function switch
    {
        TqFunction tq => new FunctionSignature(
            [..tq.Parameters.Select(parameter => TypeChecker.TypeFromExpression(parameter.Type))],
            tq.ReturnType == null ? Void() : TypeChecker.TypeFromExpression(tq.ReturnType)),

        // The generic parameters of a .NET method come first, as in a Tq function: `GC.AllocateArray(type T, int length)`.
        DotnetMethod method => new FunctionSignature(
            [
                ..method.TypeParameters.Select(_ => (ITypeRef?)new StructTypeReference(CompilerStd.Types.Type)),
                ..method.ParameterTypes.Select(type => type is UnsupportedTypeRef ? null : type),
            ],
            method.ReturnType == null ? Void() : method.ReturnType is UnsupportedTypeRef ? null : method.ReturnType),

        _ => null,
    };

    /// <summary>Same number of parameters, of the same types.</summary>
    public bool HasSameParametersAs(FunctionSignature other) =>
        Parameters.Count == other.Parameters.Count
        && Parameters.Zip(other.Parameters).All(pair => Compatible(pair.First, pair.Second));

    /// <summary>Same parameters and the same return type: one can replace the other.</summary>
    public bool IsSameAs(FunctionSignature other) => HasSameParametersAs(other) && Compatible(Return, other.Return);

    private static bool Compatible(ITypeRef? a, ITypeRef? b) =>
        a == null || b == null || DependsOnTypeParameter(a) || DependsOnTypeParameter(b) || a.Equals(b);

    /// <summary>`T`, `[]T`: what the call decides, so it matches anything here.</summary>
    private static bool DependsOnTypeParameter(ITypeRef type) => type switch
    {
        GenericParameterTypeRef => true,
        SliceTypeRef slice => DependsOnTypeParameter(slice.Element),
        ArrayTypeRef array => DependsOnTypeParameter(array.Element),
        _ => false,
    };

    private static StructTypeReference Void() => new(CompilerStd.Types.Void);

    public override string ToString() =>
        $"({string.Join(", ", Parameters.Select(TypeFormatter.Format))}) -> {TypeFormatter.Format(Return)}";
}
