namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class StringInterpolationNode : ExpressionNode
{
    public ExpressionNode Expression => (ExpressionNode)_children[1];
    public override string ToString() => $"\\{{{Expression}}}";
}
