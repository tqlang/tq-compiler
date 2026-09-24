using System.Text;

namespace Tqc.Syntax.Nodes;

public record MemberAccessExpressionNode(ExpressionNode Left, TokenNode Dot, IdentifierExpressionNode Right) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Left).AppendSyntaxNode(Dot).AppendSyntaxNode(Right);
}
