using System.Text;

namespace Tq.Ast;

public abstract class UnaryExpressionNode : ExpressionNode
{
    public static UnaryPrefixExpressionNode BuildPrefix(TokenNode operatorNode, ExpressionNode expression)
        => new (operatorNode, expression);
    
    public static UnaryPostfixExpressionNode BuildPostfix(ExpressionNode expression, TokenNode operatorNode)
        => new (expression, operatorNode);
}

public class UnaryPrefixExpressionNode : UnaryExpressionNode {
    public readonly TokenNode      Operator;
    public readonly ExpressionNode Expression;

    internal UnaryPrefixExpressionNode(TokenNode operatorNode, ExpressionNode expression)
    { 
        Operator   = operatorNode;
        Expression = expression;
    }

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        Operator.AppendStringBuilder(sb);
        Expression.AppendStringBuilder(sb);
        return sb;
    }
}

public class UnaryPostfixExpressionNode : UnaryExpressionNode {
    public readonly ExpressionNode Expression;
    public readonly TokenNode      Operator;

    internal UnaryPostfixExpressionNode(ExpressionNode expression, TokenNode operatorNode)
    { 
        Expression = expression;
        Operator   = operatorNode;
    }

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        Expression.AppendStringBuilder(sb);
        Operator.AppendStringBuilder(sb);
        return sb;
    }
}
