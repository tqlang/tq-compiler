namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class TypeCastNode : ExpressionNode
{
    public ExpressionNode Value => (ExpressionNode)_children[0];
    public ExpressionNode TargetType => (ExpressionNode)_children[2];
}