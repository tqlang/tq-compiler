namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class ElseStatementNode : StatementNode
{
    public SyntaxNode Then => _children[1];
}
