using System.Text;

namespace Tqc.Syntax.Nodes;

public record IndexCollectionNode(
    TokenNode LeftBrackets,
    (ExpressionNode node, TokenNode? comma)[] Arguments,
    TokenNode RightBrackets
) : SyntaxNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb.AppendSyntaxNode(LeftBrackets);
        foreach (var (node, comma) in Arguments)
        {
            sb.Append(node);
            if (comma != null) sb.Append(comma);
        }
        sb.AppendSyntaxNode(RightBrackets);
        return sb;
    }
}
