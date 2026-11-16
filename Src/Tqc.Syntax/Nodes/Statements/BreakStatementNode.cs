using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

/// <summary>`break`: leaves the closest loop.</summary>
public record BreakStatementNode(TokenNode BreakKeyword, TriviaNode[] LeadingTrivia) : StatementNode(LeadingTrivia)
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(BreakKeyword)
        .AppendTrivia(LeadingTrivia);

    public override SourceSpan Span => BreakKeyword.Span;
}
