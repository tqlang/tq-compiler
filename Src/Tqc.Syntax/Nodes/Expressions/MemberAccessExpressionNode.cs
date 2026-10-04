using System.Linq.Expressions;
using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record MemberAccessExpressionNode(ExpressionNode Left, TokenNode Dot, IdentifierExpressionNode Right) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Left).AppendSyntaxNode(Dot).AppendSyntaxNode(Right);
    public override SourceSpan Span => Left.Span + Right.Span;
}
