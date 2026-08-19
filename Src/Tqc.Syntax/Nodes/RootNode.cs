using System.Collections;

namespace Tqc.Syntax.Nodes;

public record RootNode : SyntaxNode, IEnumerable<SyntaxNode>
{
    internal List<SyntaxNode> _children = [];
    public IReadOnlyList<SyntaxNode> Children => _children;
    
    public IEnumerator<SyntaxNode> GetEnumerator() => _children.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
