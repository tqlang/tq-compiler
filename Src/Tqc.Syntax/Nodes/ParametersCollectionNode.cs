using System.Text;

namespace Tqc.Syntax.Nodes;

public record ParametersCollectionNode(
    TokenNode LeftParenthesis,
    (ParameterNode node, TokenNode? comma)[] Parameters,
    TokenNode RightParenthesis
) : SyntaxNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb.AppendSyntaxNode(LeftParenthesis);
        foreach (var (node, comma) in Parameters)
        {
            sb.Append(node);
            if (comma != null) sb.Append(comma);
        }
        sb.AppendSyntaxNode(RightParenthesis);
        return sb;
    }
}
