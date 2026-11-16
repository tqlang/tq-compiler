using System.Collections;
using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

/// <summary>`[a, b, c]`</summary>
public record ArrayLiteralExpressionNode(
    TokenNode LeftBracket,
    (ExpressionNode node, TokenNode? comma)[] Elements,
    TokenNode RightBracket
) : ExpressionNode, IEnumerable<ExpressionNode>
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb.AppendSyntaxNode(LeftBracket);
        foreach (var (node, comma) in Elements) sb.AppendSyntaxNode(node).AppendSyntaxNode(comma);
        return sb.AppendSyntaxNode(RightBracket);
    }

    public override SourceSpan Span => LeftBracket.Span + RightBracket.Span;

    public IEnumerator<ExpressionNode> GetEnumerator()
    {
        foreach (var (node, _) in Elements) yield return node;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
