using Tq.Core.Language.Members;
using Tq.Core.Misc;

namespace Tq.Cli.Build;

public static partial class Builder
{
    private static void ParseTqModule(TqModule module, BuildModuleConfig config)
    {
        if (!config.ExtraFields.TryGetValue("path", out var pathObject)  || pathObject is not string path || string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException($"The module '{config.Name}' is missing the required 'path' string field in config.");
        
        var projectPath = Path.GetFullPath(path);
        var rootNamespace = BuildNamespaceTreeRecursive(projectPath);
        
    }
    
    private static NamespaceNode BuildNamespaceTreeRecursive(string directoryPath)
    {
        var dirInfo = new DirectoryInfo(directoryPath);
        var dirFiles = dirInfo.GetFiles("*.tq");
        var dirDirectories = dirInfo.GetDirectories();
        
        var currentNamespace = new NamespaceNode
        {
            Name     = dirInfo.Name,
            FullPath = dirInfo.FullName
        };
        
        foreach (var file in dirFiles)
        {
            currentNamespace.Scripts.Add(new ScriptNode
            {
                Name     = file.Name,
                FullPath = file.FullName
            });
        }
        
        foreach (var subDir in dirDirectories)
        {
            if (subDir.Name.StartsWith('.') || subDir.Name.Equals("bin", StringComparison.OrdinalIgnoreCase))
                continue;

            var subNamespace = BuildNamespaceTreeRecursive(subDir.FullName);
            currentNamespace.Namespaces.Add(subNamespace);
        }

        return currentNamespace;
    }
    
    public class NamespaceNode
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        
        public List<NamespaceNode> Namespaces { get; set; } = [];
        public List<ScriptNode> Scripts { get; set; } = [];

        public override string ToString() => $"Namespace {Name}";
    }
    public class ScriptNode
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        
        public override string ToString() => $"Script {Name}";
    }
}
