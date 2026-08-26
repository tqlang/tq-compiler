namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class ElifStatementNode : StatementNode
{
    public ExpressionNode Condition => (ExpressionNode)_children[1];
    public SyntaxNode Then => _children[2];
}
