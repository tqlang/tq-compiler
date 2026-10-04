using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record ElseStatementNode(
    TokenNode ElseKeyword,
    StatementNode Statement,
    TriviaNode[] LeadingTrivia
) : StatementNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(ElseKeyword)
        .AppendSyntaxNode(Statement)
        .AppendTrivia(LeadingTrivia);
    
    public override SourceSpan Span => ElseKeyword.Span + Statement.Span;
}
