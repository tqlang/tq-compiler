using Abstract.Cli.Build;

namespace Abstract.Cli;

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
            case "build" or "b":
                DigestBuildArgs(args[1..]);
                break;
                
            case "help" or "h" or "-help" or "--help" or "-h":
                Help();
                return 0;
            
            default:
                Help();
                break;
        }
        
        return 1;
    }

    private static int DigestBuildArgs(string[] args)
    {
        if (args.Length < 1) throw new Exception("Expected program name");
        var buildOps = new BuildOptions(args[0]);

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
                case "-i" or "--include" when args.Length < i + 1:
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
        Console.WriteLine("No argument provided.");
        Console.WriteLine("Try 'help' to more details.\n");

        Console.WriteLine("Compiler options:");
        Console.WriteLine("\t- build           Builds the project (bruh)");
    }
}

