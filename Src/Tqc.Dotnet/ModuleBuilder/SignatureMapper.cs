using Mono.Cecil;

namespace Tqc.Dotnet;

/// <summary>
/// Reescreve os tipos de uma assinatura (retorno, parâmetros, campo, propriedade) para que cada tipo
/// nomeado use a Reference do builder (relativa ao assembly que exporta) em vez da referência de
/// metadata original (relativa ao assembly que define).
/// </summary>
sealed internal class SignatureRemapper(Func<TypeReference, TypeReference> mapNamed)
{
    /// <param name="declaring">Referência do tipo dono do membro (resolve os generics de tipo).</param>
    /// <param name="method">Referência do método em construção (resolve os generics de método); null fora de métodos.</param>
    public TypeReference Map(TypeReference type, TypeReference declaring, MethodReference? method) => type switch
    {
        GenericParameter gp => MapGenericParameter(gp, declaring, method),

        ArrayType array => array.Rank == 1
            ? new ArrayType(Map(array.ElementType, declaring, method))
            : new ArrayType(Map(array.ElementType, declaring, method), array.Rank),

        ByReferenceType byRef => new ByReferenceType(Map(byRef.ElementType, declaring, method)),
        PointerType pointer   => new PointerType(Map(pointer.ElementType, declaring, method)),
        PinnedType pinned     => new PinnedType(Map(pinned.ElementType, declaring, method)),

        RequiredModifierType mod => new RequiredModifierType(Map(mod.ModifierType, declaring, method), Map(mod.ElementType, declaring, method)),
        OptionalModifierType mod => new OptionalModifierType(Map(mod.ModifierType, declaring, method), Map(mod.ElementType, declaring, method)),

        GenericInstanceType instance => MapInstance(instance, declaring, method),

        // Raros e sem tipo nomeado para remapear.
        FunctionPointerType or SentinelType => type,

        _ => mapNamed(type),
    };

    private GenericInstanceType MapInstance(GenericInstanceType instance, TypeReference declaring, MethodReference? method)
    {
        var result = new GenericInstanceType(mapNamed(instance.ElementType));
        foreach (var argument in instance.GenericArguments)
            result.GenericArguments.Add(Map(argument, declaring, method));
        return result;
    }

    private static TypeReference MapGenericParameter(GenericParameter parameter, TypeReference declaring, MethodReference? method)
    {
        var owner = parameter.Type == GenericParameterType.Method && method != null
            ? method.GenericParameters
            : declaring.GenericParameters;

        return parameter.Position < owner.Count ? owner[parameter.Position] : parameter;
    }
}