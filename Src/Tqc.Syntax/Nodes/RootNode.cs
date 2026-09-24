using System.Collections;
using System.Text;

namespace Tqc.Syntax.Nodes;

public record RootNode(SyntaxNode[] Children) : SyntaxNode, IEnumerable<SyntaxNode>
{
    public IEnumerator<SyntaxNode> GetEnumerator() => Children.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        foreach (var i in Children) i.AppendSyntaxToStringBuilder(sb);
        return sb;
    }
}
