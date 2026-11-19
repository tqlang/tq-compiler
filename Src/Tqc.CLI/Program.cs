using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Tqc.Backend.Cecil;
using Tqc.Build.Model;
using Tqc.Build.Toml;
using Tqc.Diagnostics;
using Tqc.Dotnet;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
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
                Environment.Exit(Build(args[1..]));
            break;
        }
        
        Environment.Exit(1);
    }

    /// <summary>`--dump-ir` writes the IR of each Tq module as text, after type checking; `--dump-ir=dir` picks the directory.</summary>
    const string DumpIrOption = "--dump-ir";
    const string DefaultDumpDirectory = "tqir";

    static int Build(string[] allArgs)
    {
        string? dumpDirectory = null;
        var args = new List<string>();
        foreach (var argument in allArgs)
        {
            if (argument == DumpIrOption) dumpDirectory = DefaultDumpDirectory;
            else if (argument.StartsWith(DumpIrOption + "=")) dumpDirectory = argument[(DumpIrOption.Length + 1)..];
            else args.Add(argument);
        }

        var manifestFile = Path.GetFullPath(args.Count > 0 ? args[0] : "./build.toml");
        if (!File.Exists(manifestFile))
        {
            throw new NotImplementedException(
                $"File not found in path '{manifestFile}'. "
                + $"Default BuildManifest not implemented.");
        }
        
        // What the compiler creates for Std (the primitive types), next to the dumps of the modules.
        if (dumpDirectory != null) DumpMembers(CompilerStd.Module, CompilerStd.ModuleName, dumpDirectory, "compiler");

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
        List<(IModule Module, string Name)> tqModules = [];
        List<IModule> dotnetModules = [];
        
        foreach (var moduleDescriptor in order)
        {
            switch (moduleDescriptor.Type)
            {
                case "dotnet":
                    var dotnetModule = BuildDotnetModule(moduleDescriptor);
                    map.Add(moduleDescriptor, dotnetModule);
                    dotnetModules.Add(dotnetModule);
                    if (dumpDirectory != null) DumpMembers(dotnetModule, moduleDescriptor.Name, dumpDirectory);
                break;

                case "tq":
                case null:
                {
                    var module = BuildTqModule(moduleDescriptor, diagnostics);
                    // The Std of the project is merged with the one the compiler creates.
                    var view = moduleDescriptor.Name == CompilerStd.ModuleName
                        ? SemanticAnalyser.MergeStd(module, diagnostics)
                        : module;
                    map.Add(moduleDescriptor, view);
                    var dependencies = 
                            moduleDescriptor.Dependencies
                            .OfType<ModuleReference>()
                            .Select(reference => reference.Definition)
                            .Select(definition => map[definition]);
                    
                    SemanticAnalyser.BindReferences(module, view, [..dependencies], diagnostics);
                    SemanticAnalyser.TypeCheck(module, [..dependencies], diagnostics);
                    tqModules.Add((module, moduleDescriptor.Name));

                    // Written even when there are errors: that is when it is most useful.
                    if (dumpDirectory != null) DumpIr(module, moduleDescriptor.Name, dumpDirectory);
                } break;
                
                default: throw new Exception($"Unknown module type: {moduleDescriptor.Type}"); 
            }
        }
        
        // `target = "dotnet"` compiles each Tq module to an assembly in `out-dir`, if the code has no errors.
        if (!diagnostics.HasErrors && manifest.Project.Target == "dotnet")
        {
            var outDirectory = Path.Combine(Path.GetDirectoryName(manifestFile)!, manifest.OutDirectory ?? ".tq-out");
            EmitDotnet(tqModules, dotnetModules, manifest.Project.Root, outDirectory, diagnostics);
        }

        PrintDiagnostics(diagnostics);
        return diagnostics.HasErrors ? 1 : 0;
    }

    static void EmitDotnet(
        List<(IModule Module, string Name)> modules, List<IModule> dotnetModules, string root, string outDirectory, DiagnosticBag diagnostics)
    {
        var backend = new CecilBackend(diagnostics);
        foreach (var dotnetModule in dotnetModules) backend.AddDotnetModule(dotnetModule);
        foreach (var (module, name) in modules) backend.Emit(module, name, isEntry: name == root);

        if (diagnostics.HasErrors) return;

        backend.Write(outDirectory);
        Console.WriteLine($"EMITTED {modules.Count} module(s) to {outDirectory}");
    }

    /// <summary>Members of a module that is not Tq: `Name.tqir` is the one of the Tq module, so these are `Name.dotnet.tqir`.</summary>
    static void DumpMembers(IModule module, string name, string directory, string suffix = "dotnet")
    {
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, $"{name}.{suffix}.tqir");
        File.WriteAllText(file, SemanticAnalyser.DumpMembers(module, name));
        Console.WriteLine($"DUMPED {name} members to {Path.GetFullPath(file)}");
    }

    static void DumpIr(IModule module, string name, string directory)
    {
        Directory.CreateDirectory(directory);
        var file = Path.Combine(directory, name + ".tqir");
        File.WriteAllText(file, SemanticAnalyser.DumpIr(module, name));
        Console.WriteLine($"DUMPED {name} IR to {Path.GetFullPath(file)}");
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
        CollectSyntaxDiagnostics(moduleRoot, diagnostics);
        
        Console.WriteLine($"PARSED {module.Name}");
        return m;
    }

    /// <summary>TreeLower errors live in each file's tree; gather them into the build report.</summary>
    static void CollectSyntaxDiagnostics(SyntaxNamespaceContainer container, DiagnosticBag target)
    {
        foreach (var tree in container.Trees)
        foreach (var diagnostic in tree.Diagnostics)
            if (diagnostic is CompilationError error)
                target.ReportError(error);

        foreach (var child in container.Namespaces) CollectSyntaxDiagnostics(child, target);
    }

    static void PrintDiagnostics(DiagnosticBag diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            var where = diagnostic is CompilationError { Location: { } location }
                ? $"{location.filePath}({location.span.LineStart},{location.span.ColStart}): "
                : "";
            Console.Error.WriteLine($"{where}error: {diagnostic.Message}");
        }

        if (diagnostics.HasErrors) Console.Error.WriteLine($"{diagnostics.Count} error(s)");
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
        
        // Entries that do not match are ignored (see "Naming Limitations" in the language reference).
        private static readonly Regex ValidDirectoryName = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private static readonly Regex ValidFileName = new(@"^[a-zA-Z_][a-zA-Z0-9_]*\.tq$", RegexOptions.Compiled);
        
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
            {
                if (!ValidFileName.IsMatch(file.Name)) continue;
                node.Files.Add(new SourceFile(file.FullName));
            }
            
            foreach (var subDir in dirInfo.GetDirectories())
            {
                if (subDir.Name is "bin" or "obj" || !ValidDirectoryName.IsMatch(subDir.Name)) 
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
