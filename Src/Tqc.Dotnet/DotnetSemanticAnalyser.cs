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
    
    private static void ConfigureResolver(DefaultAssemblyResolver resolver)
    {
        var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;

        if (!string.IsNullOrWhiteSpace(trustedAssemblies))
        {
            foreach (var path in trustedAssemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var directory = Path.GetDirectoryName(path);
                if (directory is not null) resolver.AddSearchDirectory(directory);
            }
        }
        
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (string.IsNullOrWhiteSpace(dotnetRoot)) dotnetRoot = "/usr/share/dotnet";

        var packsDirectory = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref");
        if (!Directory.Exists(packsDirectory)) return;

        foreach (var versionDirectory in Directory.GetDirectories(packsDirectory))
        {
            var refDirectory = Path.Combine(versionDirectory, "ref", "net10.0");
            if (Directory.Exists(refDirectory)) resolver.AddSearchDirectory(refDirectory);
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
