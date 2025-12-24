namespace CodeProcess.Lexing;

public struct Token(TokenType type, uint ptr, uint len, uint line, uint column)
{
    public readonly uint pointer = ptr;
    public readonly uint length = len;
    
    public readonly TokenType Type = type;
    public readonly uint line = line;
    public readonly uint column = column;
    
    public override string ToString() => $"token [{pointer}..{length}] ({line}:{column})";
}
