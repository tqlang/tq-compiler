using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record IfStatementNode(
    TokenNode IfKeyword,
    ExpressionNode Condition,
    StatementNode Statement,
    TriviaNode[] LeadingTrivia
) : StatementNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(IfKeyword)
        .AppendSyntaxNode(Condition)
        .AppendSyntaxNode(Statement)
        .AppendTrivia(LeadingTrivia);
    
    public override SourceSpan Span => IfKeyword.Span + Statement.Span;
}
