namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class UnaryPostfixExpressionNode : ExpressionNode
{
    public override string ToString() => $"{_children[0]}{_children[1]}";
    public ExpressionNode Expression => (ExpressionNode)_children[0];
    public string Operator => ((TokenNode)_children[1]).Value;
}
