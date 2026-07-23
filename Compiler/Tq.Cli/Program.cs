using Tq.Cli.Build;
using Tq.Core;

namespace  Tq.Cli;

public static class Program
{
    public static void Main(string[] args)
    {
        PluginServer.Install();
        
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: tqc build <build.toml>");
            PrintHelp();
            Environment.Exit(1);
        }

        switch (args[0])
        {
            case "build": Build(args[1..]); return;
            case "help" or "--help" or "-h" or "h":
                PrintHelp();
                Environment.Exit(0);
                return;
        }
        
    }
    public static void Build(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: tqc build <build.toml>");
            PrintHelp();
            Environment.Exit(1);
        }

        var filePath = args[0];
        if (!File.Exists(filePath))
        {
            Console.WriteLine("File not found: " + filePath);
            Environment.Exit(1);
        }
        
        var buildConfig = ConfigParser.Parse(File.ReadAllText(filePath));
        Builder.Build(buildConfig);
    }

    public static void PrintHelp()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("    help       Show this");
        Console.WriteLine("    build      Builds the program");
    }
}