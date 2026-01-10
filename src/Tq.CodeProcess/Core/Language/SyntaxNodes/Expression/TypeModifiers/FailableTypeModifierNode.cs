namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression.TypeModifiers;

public class FailableTypeModifierNode : ExpressionNode
{
    public ExpressionNode Type => (ExpressionNode)_children[1];
}