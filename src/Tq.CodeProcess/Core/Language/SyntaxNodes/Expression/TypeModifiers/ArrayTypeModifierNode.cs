namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class ArrayTypeModifierNode : ExpressionNode
{
    private int RightBraketIndex => _children.FindIndex((e) => e is TokenNode @token && token.Value == "]");

    public ExpressionNode Type => (ExpressionNode)_children[RightBraketIndex + 1];
    public override string ToString() => string.Join("", _children);
}
