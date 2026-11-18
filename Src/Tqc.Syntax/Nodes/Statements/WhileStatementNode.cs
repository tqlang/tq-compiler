using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

/// <summary>
/// `while c s`, `while c : step s` or `while let i = 0 : c : step s`. The declaration and the step are optional.
/// </summary>
public record WhileStatementNode(
    TokenNode WhileKeyword,
    TokenNode? OpenParenthesis,
    ExpressionNode? Declaration,
    TokenNode? DeclarationColon,
    ExpressionNode Condition,
    TokenNode? StepColon,
    ExpressionNode? Step,
    TokenNode? ClosingParenthesis,
    StatementNode Statement,
    TriviaNode[] LeadingTrivia
) : StatementNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(WhileKeyword)
        .AppendSyntaxNode(OpenParenthesis)
        .AppendSyntaxNode(Declaration)
        .AppendSyntaxNode(DeclarationColon)
        .AppendSyntaxNode(Condition)
        .AppendSyntaxNode(StepColon)
        .AppendSyntaxNode(Step)
        .AppendSyntaxNode(ClosingParenthesis)
        .AppendSyntaxNode(Statement)
        .AppendTrivia(LeadingTrivia);

    public override SourceSpan Span => WhileKeyword.Span + Statement.Span;
}
