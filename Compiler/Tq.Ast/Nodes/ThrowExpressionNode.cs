using System.Text;

namespace Tq.Ast;

public class ThrowExpressionNode : ExpressionNode
{
    public readonly TokenNode ThrowToken;
    public readonly ExpressionNode Expression;
    
    private ThrowExpressionNode(TokenNode throwToken, ExpressionNode expression)
    {
        ThrowToken = throwToken;
        Expression = expression;
    }

    public static ThrowExpressionNode Build(TokenNode throwToken, ExpressionNode expression) => new (throwToken, expression);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        ThrowToken.AppendStringBuilder(sb);
        Expression.AppendStringBuilder(sb);
        return sb;
    }
}
