namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class IndexingOperatorNode : SyntaxNode
{
    public ExpressionNode[] Expressions => [..  _children[1..^1].Select(e => (ExpressionNode)e)];
    public override string ToString() => $"[{string.Join(", ", Expressions)}]";
}
