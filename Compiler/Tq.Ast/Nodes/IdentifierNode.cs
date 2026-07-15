using System.Text;

namespace Tq.Ast;

public class IdentifierNode : ExpressionNode
{
    public readonly TokenNode Identifier;

    private IdentifierNode(TokenNode identifier)
    {
        Identifier = identifier;
    }

    public static IdentifierNode Build(TokenNode identifier) => new(identifier);

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => Identifier.AppendStringBuilder(sb);
}