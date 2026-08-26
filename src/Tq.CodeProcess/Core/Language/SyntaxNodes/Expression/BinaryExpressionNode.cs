namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class BinaryExpressionNode : ExpressionNode
{
    public ExpressionNode Left => (ExpressionNode)_children[0];
    public string Operator => ((TokenNode)_children[1]).Value;
    public ExpressionNode Right => (ExpressionNode)_children[2];
}
