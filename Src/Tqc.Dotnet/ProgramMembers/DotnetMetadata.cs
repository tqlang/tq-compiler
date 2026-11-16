using Mono.Cecil;

namespace Tqc.Dotnet.ProgramMembers;

internal static class DotnetMetadata
{
    /// <summary>The definition of a type; null if its assembly cannot be loaded.</summary>
    public static TypeDefinition? Definition(TypeReference reference)
    {
        try { return reference.Resolve(); }
        catch (AssemblyResolutionException) { return null; }
    }
}
