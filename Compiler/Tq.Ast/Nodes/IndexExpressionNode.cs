using System.Text;

namespace Tq.Ast;

public class IndexExpressionNode : ExpressionNode
{
    public readonly ExpressionNode Expression;
    public readonly TokenNode LeftSquareBracketToken;
    public readonly TokenNode RightSquareBracketToken;

    public readonly (ExpressionNode expression, TokenNode? comma)[] Arguments;

    private IndexExpressionNode(
        ExpressionNode expression,
        TokenNode leftSquareBracketToken,
        (ExpressionNode expression, TokenNode? comma)[] args,
        TokenNode rightSquareBracketToken
    )
    {
        Expression            = expression;
        LeftSquareBracketToken  = leftSquareBracketToken;
        Arguments             = args;
        RightSquareBracketToken = rightSquareBracketToken;
    }

    public static IndexExpressionNode Build(
        ExpressionNode expression,
        TokenNode leftSquareBracketToken,
        (ExpressionNode expression, TokenNode? comma)[] args,
        TokenNode rightSquareBracketToken
    ) => new(expression, leftSquareBracketToken, args, rightSquareBracketToken);
    
    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        Expression.AppendStringBuilder(sb);
        LeftSquareBracketToken.AppendStringBuilder(sb);
        
        foreach (var i in Arguments)
        {
            i.expression.AppendStringBuilder(sb);
            i.comma?.AppendStringBuilder(sb);
        }

        return RightSquareBracketToken.AppendStringBuilder(sb);
    }
}
