using Abstract.CodeProcess.Core.Language.SyntaxNodes.Misc;

namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

public class FunctionCallExpressionNode : ExpressionNode
{
    public ExpressionNode FunctionReference => (ExpressionNode)_children[0];
    public ExpressionNode[] Arguments => ((ArgumentCollectionNode)_children[1]).Arguments;


    public override string ToString() => $"{FunctionReference}({string.Join(", ", Arguments)})";
}
