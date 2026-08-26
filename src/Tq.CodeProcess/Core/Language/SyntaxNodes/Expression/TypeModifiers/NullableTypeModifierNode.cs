namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class NullableTypeModifierNode : ExpressionNode
{
    public ExpressionNode Type => (ExpressionNode)_children[1];
}