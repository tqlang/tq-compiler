using Mono.Cecil;

namespace Tqc.Dotnet.ModuleBuilder;

internal static class TypeReferenceFactory
{
    public static AssemblyNameReference ScopeOf(AssemblyDefinition assembly)
    {
        var name = assembly.Name;
        return new AssemblyNameReference(name.Name, name.Version)
        {
            Culture        = name.Culture,
            PublicKeyToken = name.PublicKeyToken,
            HashAlgorithm  = name.HashAlgorithm,
        };
    }
    
    public static TypeReference Create(TypeDefinition definition, IMetadataScope scope)
    {
        var reference = new TypeReference(definition.Namespace, definition.Name, definition.Module, scope, definition.IsValueType);
        CopyGenericParameters(definition, reference);
        return reference;
    }
    
    public static TypeReference CreateNested(TypeDefinition definition, TypeReference declaringReference)
    {
        var reference = new TypeReference(string.Empty, definition.Name, definition.Module, declaringReference.Scope, definition.IsValueType)
        {
            DeclaringType = declaringReference,
        };
        CopyGenericParameters(definition, reference);
        return reference;
    }

    private static void CopyGenericParameters(TypeDefinition definition, TypeReference reference)
    {
        foreach (var parameter in definition.GenericParameters)
            reference.GenericParameters.Add(new GenericParameter(parameter.Name, reference));
    }
}
