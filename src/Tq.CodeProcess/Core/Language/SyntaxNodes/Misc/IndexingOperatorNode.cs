using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Misc;

public class IndexingOperatorNode : SyntaxNode
{
    public ExpressionNode[] Expressions => [..  _children[1..^1].Select(e => (ExpressionNode)e)];
    public override string ToString() => $"[{string.Join(", ", Expressions)}]";
}
