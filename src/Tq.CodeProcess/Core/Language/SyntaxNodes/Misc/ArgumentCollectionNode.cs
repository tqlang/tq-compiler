namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class ArgumentCollectionNode : SyntaxNode
{
    public ExpressionNode[] Arguments => [..  _children[1..^1].Select(e => (ExpressionNode)e)];

    public override string ToString() => _children.Count > 2
        ? base.ToString()
        : $"({string.Join(", ", _children[1..^1])})";
}
