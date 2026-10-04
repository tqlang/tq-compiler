using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record ExpressionStatementNode(ExpressionNode Expression, TriviaNode[] LeadingTrivia) : StatementNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Expression);
    public override SourceSpan Span => Expression.Span;
}
