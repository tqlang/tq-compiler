namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class CollectionExpressionNode : ExpressionNode
{
    public ExpressionNode[] Items => [.. _children[1..^1].Select(e => (ExpressionNode)e)];
    public override string ToString() => $"[ {string.Join(", ", Items)} ]";
}
