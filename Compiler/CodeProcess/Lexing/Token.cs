namespace CodeProcess.Lexing;

struct class Token(TokenType type, uint ptr, uint len, uint line, uint column)
{
    private readonly uint pointer = ptr;
    private readonly uint length = len;
    
    public readonly TokenType Type = type;
    public readonly uint line = line;
    public readonly uint column = column;
    
    public override string ToString() => $"token [{pointer}..{length}] ({line}:{column})";
}
