using Tqc.Build.Model;
using Tqc.Build.Toml;
using Tqc.Logging;

namespace Tqc.CLI;

static class Program
{
    static void Main(string[] args)
    {
        switch (args[0])
        {
            case "build_toml":
                Build(args[1..]);
                Environment.Exit(0);
            break;
        }
        
        Environment.Exit(1);
    }

    static void Build(string[] args)
    {
        var manifestFile = Path.GetFullPath(args.Length > 0 ? args[0] : "./build.toml");
        if (!File.Exists(manifestFile))
        {
            throw new NotImplementedException($"File not found in path '{manifestFile}'. "
                + $"Default BuildManifest not implemented.");
        }
        
        var manifest = ParseManifest(manifestFile);
        var modulesDictionary = manifest.Modules.ToDictionary(e => e.Name);

        foreach (var (_, module) in modulesDictionary)
        {
            for (var i = 0; i < module.Dependencies.Count; i++)
            {
                if (module.Dependencies[i] is not ModuleReference moduleReference) continue;
                if (!modulesDictionary.TryGetValue(moduleReference.Name, out var dep))
                    throw new Exception($"Module '{moduleReference.Name}' not found.");
                module.Dependencies[i] = new ModuleReference(moduleReference.Name, dep);
            }
        }

        var buildPlanner = new BuildPlanner();
        var order = buildPlanner.CreateOrder(modulesDictionary, manifest.Project!.Root);

        foreach (var i in order)
        {
            var scope = Logger.CreateScope($"COMPILING {i.Name}...");

            for (var j = 0; j < 10000; j++)
            {
                scope.Dbg($"Counting ({j})...");
            }
            
            scope.Close($"COMPILED {i.Name}");
        }
    }
    
    static BuildManifest ParseManifest(string manifestPath) => TomlBuild.Load(manifestPath);
    
}
