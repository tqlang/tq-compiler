namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class TopLevelVariableNode : ControlNode
{
    public bool IsConstant => ((TokenNode)_children[0]).Token.type == TokenType.ConstKeyword;
    public ExpressionNode Type => ((TypedIdentifierNode)_children[1]).Type;
    public IdentifierNode Identifier => ((TypedIdentifierNode)_children[1]).Identifier;

}
