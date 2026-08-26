namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class LocalVariableNode : ExpressionNode
{
    public bool IsConstant => ((TokenNode)_children[0]).Token.type == TokenType.ConstKeyword;
    public bool IsImplicitTyped => _children[1] is not TypedIdentifierNode;
    
    public TypedIdentifierNode TypedIdentifier => (TypedIdentifierNode)_children[1];
    public IdentifierNode Identifier => (IdentifierNode)_children[1];
}
