using System.Collections;
using System.Collections.Immutable;
using System.Text;

namespace Tqc.Syntax.Nodes;

public record BlockNode(
    TokenNode LeftBrace,
    ImmutableArray<SyntaxNode> Children,
    TokenNode RightBrace
) : SyntaxNode, IEnumerable<SyntaxNode>
{
    public IEnumerator<SyntaxNode> GetEnumerator() => Children.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        LeftBrace.AppendSyntaxToStringBuilder(sb);
        foreach (var i in Children) i.AppendSyntaxToStringBuilder(sb);
        return RightBrace.AppendSyntaxToStringBuilder(sb);
    }
}
