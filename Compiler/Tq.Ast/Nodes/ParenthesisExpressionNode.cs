using System.Text;

namespace Tq.Ast;

public class ParenthesisExpressionNode : ExpressionNode
{
    public TokenNode LeftParenthesis;
    public ExpressionNode Expression;
    public TokenNode RightParenthesis;

    private ParenthesisExpressionNode(TokenNode leftParenthesis, ExpressionNode expression, TokenNode rightParenthesis)
    {
        LeftParenthesis = leftParenthesis;
        Expression = expression;
        RightParenthesis = rightParenthesis;
    }
    
    public static ParenthesisExpressionNode Build(TokenNode leftParenthesis, ExpressionNode expression, TokenNode rightParenthesis)
        => new (leftParenthesis, expression, rightParenthesis);
    
    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        LeftParenthesis.AppendStringBuilder(sb);
        Expression.AppendStringBuilder(sb);
        RightParenthesis.AppendStringBuilder(sb);
        return sb;
    }
}