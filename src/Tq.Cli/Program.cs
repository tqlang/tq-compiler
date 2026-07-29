using Tq.Cli.Build;

namespace Tq.Cli;

public class Program
{
    public static int Main(string[] args)
    {
        return DigestArgs(args);
    }
    
    private static int DigestArgs(string[] args)
    {
        if (args.Length < 1)
        {
            Help();
            return 1;
        }

        switch (args[0])
        {
            case "build" or "b": DigestBuildArgs(false, args[1..]); break;
            case "run" or "r": DigestBuildArgs(true, args[1..]); break;
                
            case "help" or "h" or "-help" or "--help" or "-h":
                Help();
                return 0;
            
            default:
                Console.WriteLine("No argument provided.");
                Console.WriteLine();
                Help();
                break;
        }
        
        return 1;
    }

    private static int DigestBuildArgs(bool run, string[] args)
    {
        if (args.Length < 1) throw new Exception("Expected program name");
        var buildOps = new BuildOptions(args[0]) { Run = run };
        
        var i = 1;
        while(i < args.Length)
        {
            switch (args[i++])
            {
                case "-m" or "--module" when args.Length < i + 2:
                    throw new Exception("Expected module name and path");
                case "-m" or "--module":
                    var name = args[i++];
                    var path = args[i++];
                    buildOps.AppendModule(name, path);
                    break;
                
                case "-i" or "--include" when args.Length < i + 1:
                    throw new Exception("Expected include name or path");
                case "-i" or "--include":
                    var include = args[i++];
                    buildOps.AppendInclude(include);
                    break;
                
                case "-v" or "--verbose":
                    buildOps.Verbose = true;
                    break;
                
                case "-d" or "--debug":
                    var options = args[i++];
                    if (options == "all")
                    {
                        buildOps.DebugDumpParsedTrees = true;
                        buildOps.DebugDumpAnalyzerIr = true;
                        buildOps.DebugDumpCompressedModules = true;
                    }

                    var optionsList = options.Split(',');
                    foreach (var option in optionsList)
                    {
                        switch (option.Trim())
                        {
                            case "parsedTrees": buildOps.DebugDumpParsedTrees = true; break;
                            case "analyzedIR": buildOps.DebugDumpAnalyzerIr = true; break;
                            case "compressedModules": buildOps.DebugDumpCompressedModules = true; break;
                        }
                    }
                break;

                case "--":
                    buildOps.Args = args[i ..];
                    i             = args.Length;
                break;
                
                default:
                    Console.WriteLine($"Unknown argument '{args[--i]}'");
                    i++;
                    break;
            }
        }

        Builder.Execute(buildOps);
        
        return 0;
    }

    
    private static void Help()
    {
        Console.WriteLine("Compiler options:");
        Console.WriteLine("\t- build <program_name>                          Builds a project");
        Console.WriteLine("\t- run <program_name>                            Builds a project and runs the generated binary");
        Console.WriteLine();
        Console.WriteLine("Build & run options:");
        Console.WriteLine("\t- --module (-m) <module_name> <module_path>      Adds a module to the build");
        Console.WriteLine("\t- --include (-i) <assembly_name>                 Includes a dotnet assembly");
    }
}

