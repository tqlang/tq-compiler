using System.Text;

namespace Tq.Ast;

public class RangeExpressionNode : ExpressionNode {
    public readonly ExpressionNode? From;
    public readonly TokenNode      DotDot;
    public readonly ExpressionNode? To;
    public readonly TokenNode?      Colon;
    public readonly ExpressionNode? Step;

    private RangeExpressionNode(ExpressionNode? from, TokenNode dotDot, ExpressionNode? to, TokenNode? colon, ExpressionNode? step)
    {
        From = from;
        DotDot = dotDot;
        To = to;
        Colon = colon;
        Step = step;
    }
    
    public static RangeExpressionNode Build(ExpressionNode? from, TokenNode dotDot, ExpressionNode? to, TokenNode? colon, ExpressionNode? step)
        => new (from, dotDot, to, colon, step);

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => sb.Append(From).Append(DotDot).Append(To).Append(Colon).Append(Step);
}
