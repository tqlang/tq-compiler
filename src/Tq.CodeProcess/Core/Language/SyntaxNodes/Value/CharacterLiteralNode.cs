namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

public class CharacterLiteralNode(Token token, bool insideString = false) : ValueNode(token)
{

    public readonly bool InsideString = insideString;
    public string Value => Token.value.ToString();
    public override string ToString() => InsideString ? Value : $"'{Value}'";

    public string BuildCharacter()
    {
        return Value switch
        {
            // Control
            "\\t" => "\t",
            "\\n" => "\n",
            "\\r" => "\r",
            "\\0" => "\0",

            // Characters
            "\\\"" => "\"",
            "\\\'" => "\'",
            "\\\\" => "\\",

            _ => Value
        };
    }
}
