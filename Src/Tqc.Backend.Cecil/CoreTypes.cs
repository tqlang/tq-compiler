using Mono.Cecil;

namespace Tqc.Backend.Cecil;

/// <summary>
/// The types of the base library (`System.Object`, `System.Int32`...) that the emitted code uses. They are found in the
/// dotnet assemblies of the build (the reference assemblies of `System.Runtime`), so the emitted assembly refers to
/// the same assemblies as any other program.
/// </summary>
internal sealed class CoreTypes(IReadOnlyList<AssemblyDefinition> assemblies)
{
    private readonly Dictionary<string, TypeDefinition?> _cache = [];

    public TypeDefinition? Find(string fullName)
    {
        if (_cache.TryGetValue(fullName, out var found) && found != null) return found;

        TypeDefinition? result = null;
        foreach (var assembly in assemblies.OrderBy(assembly => assembly.Name.Name == "System.Runtime" ? 0 : 1))
        {
            result = assembly.MainModule.GetType(fullName);
            if (result != null) break;
        }

        _cache[fullName] = result;
        return result;
    }

    /// <summary>The type, as seen from `module`.</summary>
    public TypeReference Require(ModuleDefinition module, string fullName)
    {
        var type = Find(fullName)
                   ?? throw new BackendError(
                       $"The type '{fullName}' was not found. The build needs a dotnet module with a dependency on 'System.Runtime'");
        return module.ImportReference(type);
    }
}
