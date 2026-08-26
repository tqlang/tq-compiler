namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class TypedIdentifierNode : ExpressionNode
{
    public ExpressionNode Type => (ExpressionNode)_children[0];
    public IdentifierNode Identifier => (IdentifierNode)_children[1];
}
