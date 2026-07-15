using System.Text;

namespace Tq.Ast;

public class CallExpressionNode : ExpressionNode
{
    public readonly ExpressionNode Expression;
    public readonly TokenNode LeftParenthesisToken;
    public readonly TokenNode RightParenthesisToken;

    public readonly (ExpressionNode expression, TokenNode? comma)[] Arguments = [];

    private CallExpressionNode(
        ExpressionNode expression,
        TokenNode leftParenthesisToken,
        (ExpressionNode expression, TokenNode? comma)[] args,
        TokenNode rightParenthesisToken
    )
    {
        Expression            = expression;
        LeftParenthesisToken  = leftParenthesisToken;
        Arguments             = args;
        RightParenthesisToken = rightParenthesisToken;
    }

    public static CallExpressionNode Build(
        ExpressionNode expression,
        TokenNode leftParenthesisToken,
        (ExpressionNode expression, TokenNode? comma)[] args,
        TokenNode rightParenthesisToken
    ) => new(expression, leftParenthesisToken, args, rightParenthesisToken);
    
    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        Expression.AppendStringBuilder(sb);
        LeftParenthesisToken.AppendStringBuilder(sb);
        
        foreach (var i in Arguments)
        {
            i.expression.AppendStringBuilder(sb);
            i.comma?.AppendStringBuilder(sb);
        }

        return RightParenthesisToken.AppendStringBuilder(sb);
    }
}
