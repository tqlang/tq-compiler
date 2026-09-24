using System.Text;

namespace Tqc.Syntax.Nodes;

public record TypedefDeclaration(
    AttributeNode[] attributes,
    TokenNode TypedefKeyword,
    IdentifierExpressionNode Name,
    BlockNode Body
) : DeclarationNode(attributes) {
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendAttributesSyntaxNode(attributes)
        .AppendSyntaxNode(TypedefKeyword)
        .AppendSyntaxNode(Name)
        .AppendSyntaxNode(Body);
}

public abstract record TypedefCaseDeclaration(
    AttributeNode[] attributes,
    TokenNode CaseKeyword
) : DeclarationNode(attributes);

public record TypedefNumberedCaseDeclaration(
    AttributeNode[] attributes,
    TokenNode CaseKeyword,
    (IntegerLiteralExpressionNode value, TokenNode? comma)[] IntegerLiterals
) : TypedefCaseDeclaration(attributes, CaseKeyword)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb
            .AppendAttributesSyntaxNode(attributes)
            .AppendSyntaxNode(CaseKeyword);

        foreach (var (v, c) in IntegerLiterals)
            sb.AppendSyntaxNode(v).AppendSyntaxNode(c);
        
        return sb;
    }
}

public record TypedefNamedCaseDeclaration(
    AttributeNode[] attributes,
    TokenNode CaseKeyword,
    IdentifierExpressionNode Name
) : TypedefCaseDeclaration(attributes, CaseKeyword)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendAttributesSyntaxNode(attributes)
        .AppendSyntaxNode(CaseKeyword)
        .AppendSyntaxNode(Name);
}

public record TypedefNamedCaseWithValueDeclaration(
    AttributeNode[] attributes,
    TokenNode CaseKeyword,
    IdentifierExpressionNode Name,
    TokenNode EqualsToken,
    ExpressionNode Value
) : TypedefNamedCaseDeclaration(attributes, CaseKeyword, Name)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendAttributesSyntaxNode(attributes)
        .AppendSyntaxNode(CaseKeyword)
        .AppendSyntaxNode(Name)
        .AppendSyntaxNode(Value)
        .AppendSyntaxNode(EqualsToken);
}
