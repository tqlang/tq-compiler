using Mono.Cecil;
using Tqc.Dotnet;

namespace Tqc.Backend.Cecil;

/// <summary>
/// The types of the base library (`System.Object`, `System.Int32`...) that the emitted code uses. They are found in the
/// dotnet assemblies of the build (the reference assemblies of `System.Runtime`), so the emitted assembly refers to
/// the same assemblies as any other program. `System.Runtime` is implicit: it is used even if no module asks for it.
/// </summary>
internal sealed class CoreTypes(IReadOnlyList<AssemblyDefinition> assemblies)
{
    private readonly Dictionary<string, TypeDefinition?> _cache = [];
    private AssemblyDefinition? _runtime;

    /// <summary>`System.Runtime` is always there: the dotnet modules of the build are only searched first.</summary>
    private AssemblyDefinition Runtime => _runtime ??= DotnetSemanticAnalyser.LoadImplicitRuntime();

    /// <summary>The assembly a type was reached through when that is not the one that defines it (a type forwarder).</summary>
    private readonly Dictionary<string, AssemblyNameReference> _forwardedFrom = [];

    public TypeDefinition? Find(string fullName)
    {
        if (_cache.TryGetValue(fullName, out var found) && found != null) return found;

        TypeDefinition? result = null;
        foreach (var assembly in assemblies.OrderBy(assembly => assembly.Name.Name == "System.Runtime" ? 0 : 1))
        {
            result = FindIn(assembly, fullName);
            if (result != null) break;
        }

        try { result ??= FindIn(Runtime, fullName); }
        catch (Exception exception) when (exception is FileNotFoundException or AssemblyResolutionException)
        {
            throw new BackendError($"The assembly 'System.Runtime' was not found, so the type '{fullName}' cannot be used");
        }

        _cache[fullName] = result;
        return result;
    }

    /// <summary>
    /// The type defined by the assembly or, in an assembly that only forwards it (the `System.Runtime.dll` of an installed
    /// runtime), the type it forwards to.
    /// </summary>
    private TypeDefinition? FindIn(AssemblyDefinition assembly, string fullName)
    {
        var defined = assembly.MainModule.GetType(fullName);
        if (defined != null) return defined;

        var forwarder = assembly.MainModule.ExportedTypes.FirstOrDefault(exported => exported.FullName == fullName);
        var forwarded = forwarder?.Resolve();
        if (forwarded != null) _forwardedFrom[fullName] = TypeReferenceScope(assembly);
        return forwarded;
    }

    private static AssemblyNameReference TypeReferenceScope(AssemblyDefinition assembly) =>
        new(assembly.Name.Name, assembly.Name.Version)
        {
            Culture = assembly.Name.Culture,
            PublicKeyToken = assembly.Name.PublicKeyToken,
        };

    /// <summary>The type, as seen from `module`. A forwarded type is referred to through the assembly that forwards it.</summary>
    public TypeReference Require(ModuleDefinition module, string fullName)
    {
        var type = Find(fullName)
                   ?? throw new BackendError(
                       $"The type '{fullName}' was not found in 'System.Runtime' ({DescribeRuntime()})");

        if (!_forwardedFrom.TryGetValue(fullName, out var scope)) return module.ImportReference(type);

        var reference = new TypeReference(type.Namespace, type.Name, module, scope, type.IsValueType);
        foreach (var parameter in type.GenericParameters)
            reference.GenericParameters.Add(new GenericParameter(parameter.Name, reference));
        return reference;
    }

    private string DescribeRuntime()
    {
        try { return Runtime.MainModule.FileName ?? "unknown file"; }
        catch (Exception exception) when (exception is FileNotFoundException or AssemblyResolutionException)
        {
            return "the assembly was not loaded";
        }
    }
}
