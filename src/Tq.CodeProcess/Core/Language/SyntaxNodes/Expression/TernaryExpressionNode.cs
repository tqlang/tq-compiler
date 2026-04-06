namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

public class TernaryExpressionNode : ExpressionNode
{
    public ExpressionNode Condition => (ExpressionNode)_children[0];
    public ExpressionNode IfTrue => (ExpressionNode)_children[2];
    public ExpressionNode IfFalse => (ExpressionNode)_children[4];

    public override string ToString() => $"{Condition}\n\t? {IfTrue}\n\t: {IfFalse}";
}
