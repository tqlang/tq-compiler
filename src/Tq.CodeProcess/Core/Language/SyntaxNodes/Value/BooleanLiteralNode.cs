namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

public class BooleanLiteralNode(Token tkn) : ValueNode(tkn)
{
    public bool Value => Token.value.ToString() == "true";
    public override string ToString() => $"{Value}";
}
