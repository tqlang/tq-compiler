namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class FunctionCallExpressionNode : ExpressionNode
{
    public ExpressionNode FunctionReference => (ExpressionNode)_children[0];
    public ExpressionNode[] Arguments => ((ArgumentCollectionNode)_children[1]).Arguments;


    public override string ToString() => $"{FunctionReference}({string.Join(", ", Arguments)})";
}
