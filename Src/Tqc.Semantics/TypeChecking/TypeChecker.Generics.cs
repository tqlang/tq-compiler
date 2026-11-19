using Tqc.Core;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR;
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
        StructTypeReference { Struct: GenericInstance instance } when DependsOnTypeParameter(type) => SubstituteInstance(instance, bound),
        _ => type,
    };

    /// <summary>`List(T)` with `T` replaced: another entry of the instance table. Null if a type it needs is not known.</summary>
    private static ITypeRef? SubstituteInstance(GenericInstance instance, Dictionary<TqParameter, ITypeRef> bound)
    {
        var arguments = new List<IrExpression>();

        foreach (var argument in instance.Arguments)
        {
            if (TypeExpressions.Raw(argument) is not { } raw || !DependsOnTypeParameter(raw))
            {
                arguments.Add(argument);
                continue;
            }

            if (Substitute(raw, bound) is not { } substituted || TypeExpressions.Of(substituted, argument.Location) is not { } expression)
                return null;

            arguments.Add(expression);
        }

        return new StructTypeReference(GenericInstanceTable.Shared.GetOrAdd(instance.Definition, [..arguments]));
    }

    private static bool DependsOnTypeParameter(ITypeRef? type) => type switch
    {
        GenericParameterTypeRef => true,
        SliceTypeRef slice => DependsOnTypeParameter(slice.Element),
        StructTypeReference { Struct: GenericInstance instance } =>
            instance.Arguments.Any(argument => TypeExpressions.Raw(argument) is { } raw && DependsOnTypeParameter(raw)),
        _ => false,
    };

    /// <summary>
    /// What the type arguments of a generic type stand for in its members: for a value of `List(i8)`, the `T` of
    /// `List` is `i8`. A reference to the type has the same ones. Empty for a type that is not an instance.
    /// </summary>
    private static Dictionary<TqParameter, ITypeRef> BindingsOf(ITypeRef? type)
    {
        var bound = new Dictionary<TqParameter, ITypeRef>(ReferenceEqualityComparer.Instance);
        if (type != null && CompilerStd.Types.IsRef(type, out var referred)) type = referred;

        if (type is StructTypeReference { Struct: GenericInstance { Definition: var definition } instance })
        {
            var parameters = definition.Parameters;
            for (var i = 0; i < parameters.Count && i < instance.Arguments.Length; i++)
            {
                if (TypeFromExpression(instance.Arguments[i]) is { } argument) bound[parameters[i]] = argument;
            }
        }

        return bound;
    }

    /// <summary>The arguments of the generic type that a call is made through (`list.add(x)`, with `list` a `List(i8)`).</summary>
    private Dictionary<TqParameter, ITypeRef> ReceiverBindings(IrExpression callee) =>
        callee is IrAccess access ? BindingsOf(TypeOf(access.baseExpr)) : new(ReferenceEqualityComparer.Instance);

    /// <summary>The overload with the types of its signature as this call gives them; the same one if it is not generic.</summary>
    private static Overload Specialize(
        Overload overload, IrExpression[] arguments, SourceLocation location, Dictionary<TqParameter, ITypeRef>? receiver = null)
    {
        var generic = overload.Function switch
        {
            TqFunction tq => tq.Parameters.Any(GenericParameterTypeRef.IsTypeParameter),
            DotnetMethod dotnet => dotnet.TypeParameters.Count > 0,
            _ => false,
        };
        var fromReceiver = receiver is { Count: > 0 };
        if (!generic && !fromReceiver) return overload;

        var bound = generic ? TypeArguments(overload.Function, arguments) : new Dictionary<TqParameter, ITypeRef>(ReferenceEqualityComparer.Instance);
        if (fromReceiver)
        {
            foreach (var (parameter, type) in receiver!) bound[parameter] = type;
        }

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

        if (CalleeReference(callee) is not FunctionTypeReference { Function: var function }) return null;

        var bound = TypeArguments(function, arguments);
        foreach (var (parameter, type) in ReceiverBindings(callee)) bound[parameter] = type;
        return Substitute(returned, bound);
    }
}
