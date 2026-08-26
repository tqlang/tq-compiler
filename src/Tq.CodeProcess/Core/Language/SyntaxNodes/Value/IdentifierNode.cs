namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class IdentifierNode(Token token) : ValueNode(token)
{

    public string Value => Token.value.ToString();
    public override string ToString() => Value;
}
