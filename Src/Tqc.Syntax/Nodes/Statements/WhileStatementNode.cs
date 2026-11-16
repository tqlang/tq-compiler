using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

/// <summary>
/// `while c s`, `while c : step s` or `while let i = 0 : c : step s`. The declaration and the step are optional.
/// </summary>
public record WhileStatementNode(
    TokenNode WhileKeyword,
    ExpressionNode? Declaration,
    TokenNode? DeclarationColon,
    ExpressionNode Condition,
    TokenNode? StepColon,
    ExpressionNode? Step,
    StatementNode Statement,
    TriviaNode[] LeadingTrivia
) : StatementNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(WhileKeyword)
        .AppendSyntaxNode(Declaration)
        .AppendSyntaxNode(DeclarationColon)
        .AppendSyntaxNode(Condition)
        .AppendSyntaxNode(StepColon)
        .AppendSyntaxNode(Step)
        .AppendSyntaxNode(Statement)
        .AppendTrivia(LeadingTrivia);

    public override SourceSpan Span => WhileKeyword.Span + Statement.Span;
}
