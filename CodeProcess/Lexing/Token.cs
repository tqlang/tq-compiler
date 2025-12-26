namespace CodeProcess.Lexing;

public readonly struct Token(TokenType type, string txt, uint line, uint column)
{
    public readonly TokenType Type = type;
    public readonly string Text = txt;
    public readonly uint Line = line;
    public readonly uint Column = column;
    public uint Length => (uint)Text.Length;
    
    public override string ToString() => $"{Text} ({Line}:{Column})";
}
