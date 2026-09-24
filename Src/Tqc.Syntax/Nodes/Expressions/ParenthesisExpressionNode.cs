using System.Text;

namespace Tqc.Syntax.Nodes;

public record ParenthesisExpressionNode(TokenNode LeftParenthesis, ExpressionNode Expression, TokenNode RightParenthesis) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(LeftParenthesis).AppendSyntaxNode(Expression).AppendSyntaxNode(RightParenthesis);
}
