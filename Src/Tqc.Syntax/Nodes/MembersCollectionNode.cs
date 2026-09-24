using System.Text;

namespace Tqc.Syntax.Nodes;

public record MembersCollectionNode(
    TokenNode LeftBrace,
    (IdentifierExpressionNode node, TokenNode? comma)[] Identifiers,
    TokenNode RightBrace
) : SyntaxNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb.AppendSyntaxNode(LeftBrace);
        foreach (var (node, comma) in Identifiers)
        {
            sb.Append(node);
            if (comma != null) sb.Append(comma);
        }
        sb.AppendSyntaxNode(RightBrace);
        return sb;
    }
}
