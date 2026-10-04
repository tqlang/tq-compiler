using System.Collections;
using System.Text;
using Tqc.Core;

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
    
    public override SourceSpan Span => Children.Length > 0 ? Children[0].Span + Children[1].Span : default;
}
