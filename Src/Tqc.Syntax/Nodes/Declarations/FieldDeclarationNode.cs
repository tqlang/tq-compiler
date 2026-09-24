using System.Text;

namespace Tqc.Syntax.Nodes;

public record FieldDeclarationNode(
    AttributeNode[] attributes,
    TokenNode LetKeyword,
    ExpressionNode? Type,
    IdentifierExpressionNode Name,
    TokenNode? EqualsToken,
    ExpressionNode? Value
) : DeclarationNode(attributes)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(LetKeyword)
        .AppendSyntaxNode(Type)
        .AppendSyntaxNode(Name)
        .AppendSyntaxNode(EqualsToken)
        .AppendSyntaxNode(Value);
}
