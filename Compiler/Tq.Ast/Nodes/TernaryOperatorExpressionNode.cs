using System.Text;

namespace Tq.Ast;

public class TernaryOperatorExpressionNode : ExpressionNode
{
    public readonly ExpressionNode Left;
    public readonly TokenNode Question;
    public readonly ExpressionNode Middle;
    public readonly TokenNode Colon;
    public readonly ExpressionNode Right;

    private TernaryOperatorExpressionNode(ExpressionNode left, TokenNode question, ExpressionNode middle, TokenNode colon, ExpressionNode right)
    {
        Left = left;
        Question = question;
        Right = right;
        Colon = colon;
        Middle = middle;
    }

    public static TernaryOperatorExpressionNode Build(ExpressionNode left, TokenNode question, ExpressionNode middle, TokenNode colon, ExpressionNode right)
        => new(left, question, middle, colon, right);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        Left.AppendStringBuilder(sb);
        Question.AppendStringBuilder(sb);
        Middle.AppendStringBuilder(sb);
        Colon.AppendStringBuilder(sb);
        Right.AppendStringBuilder(sb);

        return sb;
    }
}
