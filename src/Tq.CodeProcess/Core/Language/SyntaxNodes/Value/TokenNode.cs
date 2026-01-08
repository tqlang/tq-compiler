namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

public class TokenNode(Token token) : ValueNode(token)
{
    public string Value => Token.value.ToString();
    public override string ToString() => Value;
}
