namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class ReturnStatementNode : StatementNode
{
    public bool HasExpression => _children.Count > 1;
    public ExpressionNode Expression => (ExpressionNode)_children[1];
}
