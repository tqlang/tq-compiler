using System.Linq.Expressions;
using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record ElifStatementNode(
    TokenNode ElifKeyword,
    ExpressionNode Condition,
    StatementNode Statement,
    TriviaNode[] LeadingTrivia
) : StatementNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(ElifKeyword)
        .AppendSyntaxNode(Condition)
        .AppendSyntaxNode(Statement)
        .AppendTrivia(LeadingTrivia);
    
    public override SourceSpan Span => ElifKeyword.Span + Statement.Span;
}
