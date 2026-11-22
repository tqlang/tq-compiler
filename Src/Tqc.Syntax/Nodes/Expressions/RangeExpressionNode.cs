using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

/// <summary>`a .. b`, between the brackets of an index: the elements from `a` to `b`, both included.</summary>
public record RangeExpressionNode(ExpressionNode From, TokenNode DotDot, ExpressionNode To) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) =>
        sb.AppendSyntaxNode(From).AppendSyntaxNode(DotDot).AppendSyntaxNode(To);
    public override SourceSpan Span => From.Span + To.Span;
}
