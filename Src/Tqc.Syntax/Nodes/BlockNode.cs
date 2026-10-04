using System.Collections;
using System.Collections.Immutable;
using System.Text;
using Tqc.Core;

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
    
    public override SourceSpan Span => LeftBrace.Span + RightBrace.Span;
}
