using System.Text;

namespace Tq.Ast;

public class AssignmentExpressionNode : ExpressionNode {
    public readonly ExpressionNode Left;
    public readonly TokenNode      Operator;
    public readonly ExpressionNode Right;

    private AssignmentExpressionNode(ExpressionNode left, TokenNode operatorNode, ExpressionNode right)
    {
        Left = left;
        Operator = operatorNode;
        Right = right;
    }
    
    public static AssignmentExpressionNode Build(ExpressionNode left, TokenNode operatorNode, ExpressionNode right)
        => new (left, operatorNode, right);

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => sb.Append(Left).Append(Operator).Append(Right);
}
