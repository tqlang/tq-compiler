using System.Diagnostics;
using System.Text.RegularExpressions;
using Tq.CodeProcess;
using Tq.CodeProcess.Core;
using Tq.CodeProcess.Core.Language.Module;

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
        
        var err = new ErrorHandler();
        
        var lexer = new Lexer();
        var parser = new Parser(err);
        var analyzer = new Analyzer(err);
        //var compiler = new Compiler();
        
        Console.WriteLine("Starting build...");
        var completeBuild = Stopwatch.StartNew();

        var parsingModules = Stopwatch.StartNew();
        
        List<TempModule> modules = [];
        foreach (var mod in options.Modules)
        {
            var module = new TempModule(mod.name);
            modules.Add(module);
            var mod_path = mod.path;

            Console.WriteLine($"# Processing module '{module.name}':");
            Console.Write("\tSearching for files... ");
            var singleModule = Stopwatch.StartNew();
            
            var nodes = SearchSourceFiles(
                mod_path,
                options.DirectoryQueryRegex,
                options.ScriptQueryRegex);
            
            Console.WriteLine($"Done ({singleModule.Elapsed})");
            Console.Write($"\tProcessing {nodes.Length} namespaces... ");
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
            
            Console.WriteLine($"Done ({singleModule.Elapsed})");
        }
        err.SetFileNull();
        
        parsingModules.Stop();
        Console.WriteLine($"Modules parsed ({parsingModules.Elapsed})");

        if (err.ErrorCount > 0)
        {
            err.Dump();
            Environment.Exit(1);
        }
        
        var analysis = Stopwatch.StartNew();
        var progObj = analyzer.Analyze(
            options.ProjectName,
            [.. modules],
            [.. options.Includes]);
        analysis.Stop();
        Console.WriteLine($"Analysis done ({analysis.Elapsed})");
        
        
        if (progObj == null || err.ErrorCount > 0)
        {
            err.Dump();
            Environment.Exit(1);
        }
        
        var binaryEmission = Stopwatch.StartNew();
        
        // compiler.Compile(progObj);
        
        binaryEmission.Stop();
        Console.WriteLine($"Binary emission done ({binaryEmission.Elapsed})");
        
        completeBuild.Stop();
        Console.WriteLine($"Build Finished ({completeBuild.Elapsed})");

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
