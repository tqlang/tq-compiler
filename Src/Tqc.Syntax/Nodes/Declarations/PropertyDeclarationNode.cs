using System.Text;

namespace Tqc.Syntax.Nodes;

public record PropertyDeclarationNode(
    AttributeNode[] attributes,
    TokenNode LetKeyword,
    ExpressionNode? Type,
    IdentifierExpressionNode Name,
    TokenNode LeftBracket,
    (TokenNode Keyword, TokenNode? Expression)? Get,
    (TokenNode Keyword, TokenNode? Expression)? Set,
    TokenNode RightBracket
) : DeclarationNode(attributes)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb
            .AppendSyntaxNode(LetKeyword)
            .AppendSyntaxNode(Type)
            .AppendSyntaxNode(Name)
            .AppendSyntaxNode(LeftBracket);

        if (Get.HasValue)
        {
            sb
                .AppendSyntaxNode(Get.Value.Keyword)
                .AppendSyntaxNode(Get.Value.Expression);
        }
        
        if (Set.HasValue)
        {
            sb
                .AppendSyntaxNode(Set.Value.Keyword)
                .AppendSyntaxNode(Set.Value.Expression);
        }
        
        sb.AppendSyntaxNode(RightBracket);
        return sb;
    }
}
