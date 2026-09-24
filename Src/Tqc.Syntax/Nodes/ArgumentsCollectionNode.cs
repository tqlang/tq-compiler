using System.Text;

namespace Tqc.Syntax.Nodes;

public record ArgumentsCollectionNode(
    TokenNode LeftParenthesis,
    (ExpressionNode node, TokenNode? comma)[] Arguments,
    TokenNode RightParenthesis
) : SyntaxNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
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
}
