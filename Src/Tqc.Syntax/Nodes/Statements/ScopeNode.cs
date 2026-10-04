using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record ScopeNode(
    TokenNode LeftBrace,
    ImmutableArray<StatementNode> Children,
    TokenNode RightBrace,
    TriviaNode[] LeadingTrivia
) : StatementNode(LeadingTrivia), IEnumerable<StatementNode>
{
    public IEnumerator<StatementNode> GetEnumerator() => Children.AsEnumerable().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        LeftBrace.AppendSyntaxToStringBuilder(sb);
        foreach (var i in Children) i.AppendSyntaxToStringBuilder(sb);
        return RightBrace.AppendSyntaxToStringBuilder(sb);
    }
    
    public override SourceSpan Span => LeftBrace.Span + RightBrace.Span;
}
