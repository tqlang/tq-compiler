using System.Text;

namespace Tq.Ast;

public class NullLiteralNode: ExpressionNode
{
    public TokenNode Value;
    
    private NullLiteralNode(TokenNode value)
    {
        Value = value;
    }

    public static NullLiteralNode Build(TokenNode value) => new (value);

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => Value.AppendStringBuilder(sb);
}
