namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class ReferenceTypeModifierNode : ExpressionNode
{
    public ExpressionNode Type => (ExpressionNode)_children[1];
}
