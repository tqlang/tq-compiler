using System.Diagnostics;
using System.Reflection;
using Tqc.Build.Model;
using Tqc.Build.Toml;
using Tqc.Diagnostics;
using Tqc.Dotnet;
using Tqc.IR.ProgramMembers;
using Tqc.Semantics;
using Tqc.Syntax;

namespace Tqc.CLI;

static class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: tqc <operation> <...>");
            Environment.Exit(1);
        }
        
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
            throw new NotImplementedException(
                $"File not found in path '{manifestFile}'. "
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
        Dictionary<ModuleDefinition, IModule> map = []; 
        DiagnosticBag diagnostics = new();
        
        foreach (var moduleDescriptor in order)
        {
            switch (moduleDescriptor.Type)
            {
                case "dotnet":
                    map.Add(moduleDescriptor, BuildDotnetModule(moduleDescriptor));
                break;

                case "tq":
                case null:
                {
                    var module = BuildTqModule(moduleDescriptor, diagnostics);
                    map.Add(moduleDescriptor, module);
                    var dependencies = moduleDescriptor.Dependencies
                        .OfType<ModuleReference>()
                        .Select(reference => reference.Definition)
                        .Select(definition => map[definition]);
                    SemanticAnalyser.BindReferences(module, [..dependencies]);
                } break;
                
                default: throw new Exception($"Unknown module type: {moduleDescriptor.Type}"); 
            }
        }
        
    }

    static IModule BuildTqModule(ModuleDefinition module, DiagnosticBag diagnostics)
    {
        Console.WriteLine($"COMPILING {module.Name}...");
        
        if (!module.ExtraFields.TryGetValue("path", out var pathobj) || pathobj is not string path)
            throw new UnreachableException($"Expected 'path' field with value string inside '{module.Name}' module");

        var tree = DirectoryParser.BuildTree(path, module.Name);
        var moduleRoot = ParseNamespaceRecursively(tree, diagnostics);
        var syntaxModuleContainer = new SyntaxModuleContainer(module.Name, moduleRoot);
        var m = SemanticAnalyser.Resolve(syntaxModuleContainer);
        
        Console.WriteLine($"PARSED {module.Name}");
        return m;
    }

    static IModule BuildDotnetModule(ModuleDefinition module)
    {
        var m = DotnetSemanticAnalyser.AnalyseDotnetModule(module.Name, [.. module.Dependencies.OfType<DotnetAssemblyReference>()]);
        Console.WriteLine($"PARSED {module.Name}");
        return m;
    }
    
    
    private static class DirectoryParser
    {
        private const string Extension = "*.tq"; 
        public static NamespaceNode BuildTree(string rootPath, string moduleName)
        {
            var rootInfo = new DirectoryInfo(Path.GetFullPath(rootPath));
            if (!rootInfo.Exists) throw new DirectoryNotFoundException($"Directory in path '{rootPath}' not found");
            
            var rootNode = new NamespaceNode(rootInfo.Name, moduleName);
            PopulateNode(rootNode, rootInfo, moduleName);
            return rootNode;
        }
        private static void PopulateNode(NamespaceNode node, DirectoryInfo dirInfo, string currentNamespace)
        {
            foreach (var file in dirInfo.GetFiles(Extension))
                node.Files.Add(new SourceFile(file.FullName));
            
            foreach (var subDir in dirInfo.GetDirectories())
            {
                if (subDir.Name.StartsWith('.') || subDir.Name == "bin" || subDir.Name == "obj") 
                    continue;
                
                var nextNamespace = string.IsNullOrEmpty(currentNamespace) 
                    ? subDir.Name 
                    : $"{currentNamespace}.{subDir.Name}";

                var childNode = new NamespaceNode(subDir.Name, nextNamespace);
                node.SubNamespaces.Add(childNode);
            
                PopulateNode(childNode, subDir, nextNamespace);
            }
        }
    }
    
    static SyntaxNamespaceContainer ParseNamespaceRecursively(NamespaceNode node, DiagnosticBag diagnostics)
    {
        var container = new SyntaxNamespaceContainer(node.Name);
        
        foreach (var file in node.Files)
        {
            var parser = new Parser(diagnostics);
            var sourceText = File.ReadAllText(file.FilePath);
            var syntaxTree = parser.Parse(file.FilePath, sourceText);
            container.Trees.Add(syntaxTree);
        }
        
        foreach (var subNamespace in node.SubNamespaces)
            container.Namespaces.Add(ParseNamespaceRecursively(subNamespace, diagnostics));

        container.Namespaces.TrimExcess();
        container.Trees.TrimExcess();
        return container;
    }
    static BuildManifest ParseManifest(string manifestPath) => TomlBuild.Load(manifestPath);
    
}
