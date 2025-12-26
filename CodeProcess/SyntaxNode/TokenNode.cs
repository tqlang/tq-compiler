using CodeProcess.Lexing;

namespace CodeProcess.SyntaxNode;

public class TokenNode(Token tkn) : SyntaxNode
{
    public readonly Token Token = tkn;
    public TokenType Type => Token.Type;
    public override Token[] Tokens => [Token];

    public override string ToString() => Token.ToString();
}
