using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record ReturnStatementNode(
    TokenNode ReturnKeyword,
    ExpressionNode? Expression,
    TriviaNode[] LeadingTrivia
) : StatementNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(ReturnKeyword)
        .AppendSyntaxNode(Expression)
        .AppendTrivia(LeadingTrivia);
    
    public override SourceSpan Span => Expression != null ? ReturnKeyword.Span + Expression.Span : ReturnKeyword.Span;
}
