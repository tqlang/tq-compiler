using System.Text;

namespace Tq.Ast;

public class TokenNode(TriviaNode[] trivia, Token token): SyntaxNode {
    public readonly TriviaNode[] Trivia = trivia;
    public readonly Token        Token  = token;
    public          TokenType    Type  => Token.Type;
    public          string?      Value => Token.Value;

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        sb.AppendJoin("", Trivia).Append(Token.ValueAsString());
        return sb;
    }
}
