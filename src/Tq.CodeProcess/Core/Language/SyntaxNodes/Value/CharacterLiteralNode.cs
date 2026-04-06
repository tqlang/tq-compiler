using System.Globalization;

namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

public class CharacterLiteralNode(Token token, bool insideString = false) : ValueNode(token)
{

    public readonly bool InsideString = insideString;
    public string Value => Token.value.ToString();
    public override string ToString() => InsideString ? Value : $"'{Value}'";

    public string BuildCharacter()
    {
        if (Value.StartsWith("\\x")) return "" + (char)byte.Parse(Value[2..], NumberStyles.HexNumber);
        
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
