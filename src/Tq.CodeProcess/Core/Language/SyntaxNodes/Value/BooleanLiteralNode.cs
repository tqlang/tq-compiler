namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class BooleanLiteralNode(Token tkn) : ValueNode(tkn)
{
    public bool Value => Token.value.ToString() == "true";
    public override string ToString() => $"{Value}";
}
