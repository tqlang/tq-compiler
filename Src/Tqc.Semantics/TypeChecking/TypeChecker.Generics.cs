using Tqc.Core;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// A function whose first parameters are of the type `type` is generic: `func copy(type T, []T from, []T to)`.
/// The types are given first in the call (`copy(i8, a, b)`) and stand for `T` in the rest of the signature.
/// </summary>
internal sealed partial class TypeChecker
{
    /// <summary>The type that each `type` parameter of the function stands for in this call.</summary>
    private static Dictionary<TqParameter, ITypeRef> TypeArguments(IFunction function, IReadOnlyList<IrExpression> arguments)
    {
        var bound = new Dictionary<TqParameter, ITypeRef>(ReferenceEqualityComparer.Instance);

        // The types of a .NET method are all of its first parameters; those of a Tq function are the ones of the type `type`.
        IReadOnlyList<TqParameter> parameters = function switch
        {
            TqFunction tq => tq.Parameters,
            DotnetMethod dotnet => dotnet.TypeParameters,
            _ => [],
        };

        for (var i = 0; i < parameters.Count && i < arguments.Count; i++)
        {
            if (GenericParameterTypeRef.IsTypeParameter(parameters[i]) && TypeFromExpression(arguments[i]) is { } type)
                bound[parameters[i]] = type;
        }

        return bound;
    }

    /// <summary>
    /// The type with each `T` replaced by what the call gave; null when it depends on a type that is not known
    /// (the call has not said it, or it is not a type).
    /// </summary>
    private static ITypeRef? Substitute(ITypeRef? type, Dictionary<TqParameter, ITypeRef> bound) => type switch
    {
        GenericParameterTypeRef generic => bound.GetValueOrDefault(generic.Parameter),
        SliceTypeRef slice => Substitute(slice.Element, bound) is { } element ? new SliceTypeRef(element) : null,
        _ => type,
    };

    private static bool DependsOnTypeParameter(ITypeRef? type) => type switch
    {
        GenericParameterTypeRef => true,
        SliceTypeRef slice => DependsOnTypeParameter(slice.Element),
        _ => false,
    };

    /// <summary>The overload with the types of its signature as this call gives them; the same one if it is not generic.</summary>
    private static Overload Specialize(Overload overload, IrExpression[] arguments, SourceLocation location)
    {
        var generic = overload.Function switch
        {
            TqFunction tq => tq.Parameters.Any(GenericParameterTypeRef.IsTypeParameter),
            DotnetMethod dotnet => dotnet.TypeParameters.Count > 0,
            _ => false,
        };
        if (!generic) return overload;

        var bound = TypeArguments(overload.Function, arguments);
        var types = new ITypeRef?[overload.ParameterTypes.Count];
        var expressions = new IrExpression?[types.Length];

        for (var i = 0; i < types.Length; i++)
        {
            var original = overload.ParameterTypes[i];
            if (!DependsOnTypeParameter(original))
            {
                types[i] = original;
                expressions[i] = overload.ParameterTypeExpressions[i];
                continue;
            }

            types[i] = Substitute(original, bound);
            expressions[i] = types[i] is { } substituted ? TypeExpressionOf(substituted, location) : null;
        }

        return overload with { ParameterTypes = types, ParameterTypeExpressions = expressions };
    }

    /// <summary>What a call to the function returns: its return type, with the types the call gave in place of `T`.</summary>
    private ITypeRef? ReturnTypeOf(IrExpression callee, IrExpression[] arguments)
    {
        var returned = ReturnTypeOf(callee);
        if (!DependsOnTypeParameter(returned)) return returned;

        return CalleeReference(callee) is FunctionTypeReference { Function: var function }
            ? Substitute(returned, TypeArguments(function, arguments))
            : null;
    }
}
