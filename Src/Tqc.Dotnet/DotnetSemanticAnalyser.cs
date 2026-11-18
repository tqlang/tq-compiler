using Mono.Cecil;
using Tqc.Build.Model;
using Tqc.Dotnet.ModuleBuilder;
using Tqc.IR.ProgramMembers;

namespace Tqc.Dotnet;

public static class DotnetSemanticAnalyser
{
    public static IModule AnalyseDotnetModule(string moduleName, DotnetAssemblyReference[] assemblies)
    {
        using var resolver = new DefaultAssemblyResolver();
        ConfigureResolver(resolver);
        
        var assemblyDefinitions = assemblies
            .Select(a => FindAssembly(resolver, a.Name, a.Version)).ToArray();
        
        var moduleBuilder = new DotnetModuleBuilder(assemblyDefinitions);
        return moduleBuilder.Build(moduleName);
    }
    
    /// <summary>The runtime every .NET program has: `System.Runtime`, without being asked for by a module of the build.</summary>
    public const string RuntimeAssemblyName = "System.Runtime";

    private static AssemblyDefinition? _implicitRuntime;

    /// <summary>
    /// `System.Runtime` (its reference assembly, found as for any dotnet module), for what the compiler itself needs of it
    /// (`Object`, `Int32`...). Loaded once.
    /// </summary>
    public static AssemblyDefinition LoadImplicitRuntime()
    {
        if (_implicitRuntime != null) return _implicitRuntime;

        // Not disposed: the assembly reads from it as long as it is used.
        var resolver = new DefaultAssemblyResolver();
        ConfigureResolver(resolver);
        return _implicitRuntime = FindAssembly(resolver, RuntimeAssemblyName, new Version(10, 0, 0, 0));
    }

    /// <summary>
    /// The reference assemblies come first: they define the types (`System.Runtime.dll` of the runtime only forwards them
    /// to `System.Private.CoreLib`). The assemblies of the running runtime are the last resort.
    /// </summary>
    private static void ConfigureResolver(DefaultAssemblyResolver resolver)
    {
        foreach (var directory in ReferenceAssemblyDirectories()) resolver.AddSearchDirectory(directory);

        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrWhiteSpace(trustedAssemblies)) return;

        foreach (var path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var directory = Path.GetDirectoryName(path);
            if (directory is not null) resolver.AddSearchDirectory(directory);
        }
    }

    /// <summary>
    /// `packs/Microsoft.NETCore.App.Ref/&lt;version&gt;/ref/net&lt;x&gt;` of every .NET installation that can be found, newest first:
    /// `DOTNET_ROOT`, the one the compiler is running on, and the usual places of each system.
    /// </summary>
    private static IEnumerable<string> ReferenceAssemblyDirectories()
    {
        var roots = new List<string?>
        {
            Environment.GetEnvironmentVariable("DOTNET_ROOT"),
            Environment.GetEnvironmentVariable("DOTNET_ROOT(x86)"),
        };

        // .../dotnet/shared/Microsoft.NETCore.App/<version>/ : three levels up is the installation.
        try
        {
            var runtime = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory()
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            roots.Add(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(runtime))));
        }
        catch (Exception exception) when (exception is ArgumentException or PathTooLongException) { }

        roots.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) is { Length: > 0 } programFiles
            ? Path.Combine(programFiles, "dotnet") : null);
        roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet"));
        roots.AddRange(["/usr/share/dotnet", "/usr/lib/dotnet", "/usr/local/share/dotnet", "/opt/dotnet"]);

        var seen = new HashSet<string>();
        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root)) continue;

            var packs = Path.Combine(root, "packs", "Microsoft.NETCore.App.Ref");
            if (!seen.Add(packs) || !Directory.Exists(packs)) continue;

            var versions = Directory.GetDirectories(packs)
                .OrderByDescending(directory => Version.TryParse(Path.GetFileName(directory).Split('-')[0], out var v) ? v : new Version());

            foreach (var version in versions)
            {
                var reference = Path.Combine(version, "ref");
                if (!Directory.Exists(reference)) continue;

                foreach (var framework in Directory.GetDirectories(reference).OrderByDescending(name => Path.GetFileName(name)))
                    yield return framework;
            }
        }
    }

    private static AssemblyDefinition FindAssembly(DefaultAssemblyResolver resolver, string nameOrPath, Version version)
    {
        var parameters = new ReaderParameters
        {
            AssemblyResolver = resolver,
            ReadSymbols      = false
        };

        if (File.Exists(nameOrPath))
            return AssemblyDefinition.ReadAssembly(Path.GetFullPath(nameOrPath), parameters);

        var simpleName = nameOrPath;
        var reference = new AssemblyNameReference(simpleName, version);
        var assembly = resolver.Resolve(reference, parameters);

        if (!string.Equals(assembly.Name.Name, simpleName, StringComparison.OrdinalIgnoreCase))
        {
            assembly.Dispose();
            throw new FileNotFoundException($"Resolver returned '{assembly.Name.Name}' but '{simpleName}' was expected.");
        }

        return assembly;
    }
}
