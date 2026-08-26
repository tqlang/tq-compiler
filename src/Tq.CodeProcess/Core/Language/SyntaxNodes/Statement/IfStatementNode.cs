namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class IfStatementNode : StatementNode
{
    public ExpressionNode Condition => (ExpressionNode)_children[1];
    public SyntaxNode Then => _children[2];
}
