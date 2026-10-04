using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record FieldDeclarationNode(
    TokenNode LetKeyword,
    ExpressionNode? Type,
    IdentifierExpressionNode Name,
    TokenNode? EqualsToken,
    ExpressionNode? Value,
    TriviaNode[] LeadingTrivia
) : DeclarationNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(LetKeyword)
        .AppendSyntaxNode(Type)
        .AppendSyntaxNode(Name)
        .AppendSyntaxNode(EqualsToken)
        .AppendSyntaxNode(Value)
        .AppendTrivia(LeadingTrivia);

    public override SourceSpan Span => LetKeyword.Span + SourceSpan.FirstAndLastNotNull(Name, Value);
}
