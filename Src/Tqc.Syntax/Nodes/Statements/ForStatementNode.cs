using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

/// <summary>`for item in collection s` or `for item, index in collection s`.</summary>
public record ForStatementNode(
    TokenNode ForKeyword,
    TokenNode? OpenParenthesis,
    IdentifierExpressionNode Item,
    TokenNode? Comma,
    IdentifierExpressionNode? Index,
    TokenNode InKeyword,
    ExpressionNode Collection,
    TokenNode? ClosingParenthesis,
    StatementNode Statement,
    TriviaNode[] LeadingTrivia
) : StatementNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(ForKeyword)
        .AppendSyntaxNode(OpenParenthesis)
        .AppendSyntaxNode(Item)
        .AppendSyntaxNode(Comma)
        .AppendSyntaxNode(Index)
        .AppendSyntaxNode(InKeyword)
        .AppendSyntaxNode(Collection)
        .AppendSyntaxNode(ClosingParenthesis)
        .AppendSyntaxNode(Statement)
        .AppendTrivia(LeadingTrivia);

    public override SourceSpan Span => ForKeyword.Span + Statement.Span;
}
