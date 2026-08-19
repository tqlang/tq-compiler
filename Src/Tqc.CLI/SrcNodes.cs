namespace Tqc.CLI;

public class SourceFile
{
    public string FilePath { get; init; }
    public string FileName { get; init; }
    
    public SourceFile(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
    }
}

public class NamespaceNode
{
    public string Name { get; }
    public string FullNamespace { get; }
    public List<NamespaceNode> SubNamespaces { get; } = [];
    public List<SourceFile> Files { get; } = [];

    public NamespaceNode(string name, string fullNamespace)
    {
        Name          = name;
        FullNamespace = fullNamespace;
    }
}
