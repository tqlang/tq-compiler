using System.Diagnostics;
using System.Text.RegularExpressions;
using Abstract.CodeProcess;
using Abstract.CodeProcess.Core;
using Tq.CodeProcess;
using Analyser = Tq.CodeProcess.Analyser;
using Compiler = Tq.CodeProcess.Compiler;
using Module = Tq.CodeProcess.Core.Language.Module.Module;

namespace Abstract.Cli.Build;

public static class Builder
{

    private static RegexOptions _regexOptions = RegexOptions.Singleline;
    private static TimeSpan _regexTimeout = TimeSpan.FromMilliseconds(250);
    
    public static void Execute(BuildOptions options)
    {   
        // The best is make sure that all the build cache directories
        // are in the right place
        SetupBuildCache();
        
        var verbose = options.Verbose;
        
        var err = new ErrorHandler();
        
        var lexer = new Lexer();
        var parser = new Parser(err);
        var analyzer = new Analyser(err);
        var compiler = new Compiler();
        
        if (verbose) Console.WriteLine("Starting build...");
        var completeBuild = Stopwatch.StartNew();

        var parsingModules = Stopwatch.StartNew();
        
        List<Module> modules = [];
        foreach (var mod in options.Modules)
        {
            var module = new Module(mod.name);
            modules.Add(module);
            var mod_path = mod.path;

            if (verbose) Console.WriteLine($"# Processing module '{module.name}':");
            if (verbose) Console.Write("\tSearching for files... ");
            var singleModule = Stopwatch.StartNew();
            
            var nodes = SearchSourceFiles(
                mod_path,
                options.DirectoryQueryRegex,
                options.ScriptQueryRegex);
            
            if (verbose) Console.WriteLine($"Done ({singleModule.Elapsed})");
            if (verbose) Console.Write($"\tProcessing {nodes.Length} namespaces... ");
            singleModule.Restart();
            
            foreach (var (dir, scripts) in nodes)
            {
                var namespaceName = dir[mod_path.Length..]
                    .Trim(Path.DirectorySeparatorChar)
                    .Replace(Path.DirectorySeparatorChar, '.');

                var namespaceNode = module.AddNamespace(namespaceName);
                
                foreach (var i in scripts)
                {
                    err.SetFile(i);
                    var fileContent = File.ReadAllText(i);
                    var lex = lexer.Lex(fileContent);
                    var tree = parser.Parse(i, lex);
                    
                    namespaceNode.AddTree(tree);
                }
                
                if (options.DebugDumpParsedTrees)
                {
                    List<string> nmsp = [ ];
                    if (!string.IsNullOrEmpty(module.name)) nmsp.AddRange(module.name.Split('.'));
                    if (!string.IsNullOrEmpty(namespaceName)) nmsp.AddRange(namespaceName.Split('.'));
                        
                    File.WriteAllText(
                        $"./.tq-cache/debug/{string.Join('.', nmsp)}.generated.a",
                        namespaceNode.ContentToString());
                }

            }
            
            if (verbose) Console.WriteLine($"Done ({singleModule.Elapsed})");
        }
        err.SetFileNull();
        
        parsingModules.Stop();
        if (verbose) Console.WriteLine($"Modules parsed ({parsingModules.Elapsed})");

        if (err.ErrorCount > 0)
        {
            err.Dump();
            Environment.Exit(1);
        }
        
        var analysis = Stopwatch.StartNew();
        var progObj = analyzer.Analyze(
            options.ProjectName,
            [.. modules],
            [.. options.Includes],
            dumpGlobalTable: options.DebugDumpAnalyzerIr,
            dumpEvaluatedData: options.DebugDumpAnalyzerIr);
        analysis.Stop();
        Console.WriteLine($"Analysis done ({analysis.Elapsed})");
        
        var binaryEmission = Stopwatch.StartNew();
        
        if (progObj == null || err.ErrorCount > 0)
        {
            err.Dump();
            Environment.Exit(1);
        }
        
        compiler.Compile(progObj);
        
        binaryEmission.Stop();
        Console.WriteLine($"Binary emission done ({binaryEmission.Elapsed})");
        
        completeBuild.Stop();
        if (verbose) Console.WriteLine($"Build Finished ({completeBuild.Elapsed})");

        if (!options.Run) return;

        string binaryPath = $"./.tq-out/{options.ProjectName}.dll";
        Console.WriteLine($"Executing '{binaryPath}'...\n");
        ExecuteProgram(binaryPath, options.Args ?? []);
    }

    private static void ExecuteProgram(string binaryPath, string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName        = "dotnet",
            UseShellExecute = false,
        };

        psi.ArgumentList.Add(binaryPath);
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        Console.Clear();
        using var process = Process.Start(psi)!;
        process.WaitForExit();

        Console.ForegroundColor = process.ExitCode == 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"\nProgram exited with code {process.ExitCode}.");
        Console.ResetColor();
    }
    
    private static void SetupBuildCache()
    {
        string[] directories = [
            ".tq-out",
            
            ".tq-cache",
            ".tq-cache/debug",
            ".tq-cache/debug/realizer",
            ".tq-cache/temp",
            ".tq-cache/modules",
        ];
        string[] reset = [
            ".tq-cache/debug",
            ".tq-cache/temp",
        ];

        foreach (var i in reset)
            if (Directory.Exists(i)) Directory.Delete(i, true);
        
        foreach (var i in directories)
            if (!Directory.Exists(i)) Directory.CreateDirectory(i);
    }
    
    private static (string, string[])[] SearchSourceFiles(
        string moduleDirectory,
        string directorySearchPattern,
        string fileSearchPattern
    )
    {
        List<(string, string[])> scripts = [];

        Queue<string> queue = new();
        queue.Enqueue(moduleDirectory);
        
        while (queue.Count > 0)
        {
            var parent = queue.Dequeue();
            
            var directories = Directory.GetDirectories(parent);
            foreach (var d in directories)
            {
                var i = Path.GetFileName(d);
                if (Regex.IsMatch(i, directorySearchPattern, _regexOptions, _regexTimeout)) queue.Enqueue(d);
            }
            
            var files = Directory.GetFiles(parent);
            var match = from f in files
                let i = Path.GetFileName(f)
                where Regex.IsMatch(i, fileSearchPattern, _regexOptions, _regexTimeout)
                select f;
            
            scripts.AddRange((parent, [.. match]));
        }
        
        return [.. scripts];
    }
    
}
