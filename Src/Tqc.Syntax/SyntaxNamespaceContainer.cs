namespace Tqc.Syntax;

public record SyntaxNamespaceContainer(string Name)
{
    public readonly List<SyntaxNamespaceContainer> Namespaces = []; 
    public readonly List<SyntaxTree> Trees = [];
}
