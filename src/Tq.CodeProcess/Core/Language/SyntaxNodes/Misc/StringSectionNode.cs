namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class StringSectionNode(Token tkn) : ValueNode(tkn)
{
    public string Value => Token.value.ToString();
    public override string ToString() => Value;
}
