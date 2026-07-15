using System.Text;

namespace Tq.Ast;

public class TriviaNode(Token token): SyntaxNode {
    public readonly Token Token = token;

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => sb.Append(Token.ValueAsString());
}
