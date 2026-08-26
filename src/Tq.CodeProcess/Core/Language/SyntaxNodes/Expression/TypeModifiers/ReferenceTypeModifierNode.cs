namespace Tq.CodeProcess.Core.Language.SyntaxNodes.TypeModifiers;

public class ReferenceTypeModifierNode : ExpressionNode
{
    public ExpressionNode Type => (ExpressionNode)_children[1];
}
