using System.Collections;
using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record ArgumentsCollectionNode(
    TokenNode LeftParenthesis,
    (ExpressionNode node, TokenNode? comma)[] Arguments,
    TokenNode RightParenthesis
) : SyntaxNode, IEnumerable<ExpressionNode>
{
    internal override StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        LeftParenthesis.AppendSyntaxToStringBuilder(sb);
        foreach (var (node, comma) in Arguments)
        {
            sb.Append(node);
            if (comma != null) sb.Append(comma);
        }
        RightParenthesis.AppendSyntaxToStringBuilder(sb);
        return sb;
    }
    
    public override SourceSpan Span => LeftParenthesis.Span + RightParenthesis.Span;
    
    public IEnumerator<ExpressionNode> GetEnumerator()
    {
        foreach (var (node, _) in Arguments) yield return node;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
