namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class UnaryPrefixExpressionNode : ExpressionNode
{
    public override string ToString() => $"{_children[0]}{_children[1]}";
    public string Operator => ((TokenNode)_children[0]).Value;
    public ExpressionNode Expression => (ExpressionNode)_children[1];
}
