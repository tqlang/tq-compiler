using System.Text;

namespace Tq.Ast;

public class CastExpressionNode: ExpressionNode
{
    public readonly ExpressionNode Expression;
    public readonly TokenNode Operator;
    public readonly ExpressionNode Type;

    private CastExpressionNode(ExpressionNode expression, TokenNode operatorNode, ExpressionNode type)
    {
        expression = expression;
        Operator   = operatorNode;
        type       = Type;
    }

    public static CastExpressionNode Build(ExpressionNode expression, TokenNode operatorNode, ExpressionNode type)
        => new(expression, operatorNode, type);

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => sb.Append(Expression).Append(Operator).Append(Type);
}