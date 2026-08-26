namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class ParenthesisExpressionNode : ExpressionNode
{
    public ExpressionNode Content => (ExpressionNode)_children[1];
    public override string ToString() => $"({(ExpressionNode)_children[1]})";
}
