using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

/// <summary>`case 1, 2 => value`, `case 3 => { ... }` or `default => value`.</summary>
public record MatchArmNode(
    TokenNode Keyword,
    (ExpressionNode node, TokenNode? comma)[] Patterns,
    TokenNode Arrow,
    SyntaxNode Body
) : SyntaxNode
{
    public bool IsDefault => Keyword.Value == "default";

    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb.AppendSyntaxNode(Keyword);
        foreach (var (node, comma) in Patterns) sb.AppendSyntaxNode(node).AppendSyntaxNode(comma);
        return sb.AppendSyntaxNode(Arrow).AppendSyntaxNode(Body);
    }

    public override SourceSpan Span => Keyword.Span + Body.Span;
}

/// <summary>`match value { case 1 => a  default => b }`</summary>
public record MatchExpressionNode(
    TokenNode Keyword,
    ExpressionNode Subject,
    TokenNode LeftBrace,
    MatchArmNode[] Arms,
    TokenNode RightBrace
) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb.AppendSyntaxNode(Keyword).AppendSyntaxNode(Subject).AppendSyntaxNode(LeftBrace);
        foreach (var arm in Arms) sb.AppendSyntaxNode(arm);
        return sb.AppendSyntaxNode(RightBrace);
    }

    public override SourceSpan Span => Keyword.Span + RightBrace.Span;
}
