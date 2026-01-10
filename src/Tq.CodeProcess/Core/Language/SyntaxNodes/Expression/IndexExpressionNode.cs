using Abstract.CodeProcess.Core.Language.SyntaxNodes.Misc;

namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

public class IndexExpressionNode : ExpressionNode
{
    public ExpressionNode Target => (ExpressionNode)_children[0];
    public IndexingOperatorNode Indexer => (IndexingOperatorNode)_children[1];

    public override string ToString() => $"{Target}{Indexer}";
}