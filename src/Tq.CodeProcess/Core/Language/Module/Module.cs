using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess.Core.Language.Module;

public sealed class Module(string name)
{
    public readonly string name = name;
    
    private List<NamespaceNode> _namespaces = [];
    public NamespaceNode[] Namespaces => [.. _namespaces];

    public NamespaceNode AddNamespace(string name)
    {
        var node = new NamespaceNode(name);
        _namespaces.Add(node);
        return node;
    }

    public override string ToString() => name;
}