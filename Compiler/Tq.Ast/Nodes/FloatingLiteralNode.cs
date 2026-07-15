using System.Text;

namespace Tq.Ast;

public class FloatingLiteralNode: ExpressionNode
{
    public TokenNode Value;
    
    private FloatingLiteralNode(TokenNode value)
    {
        Value = value;
    }

    public static FloatingLiteralNode Build(TokenNode value) => new (value);

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => Value.AppendStringBuilder(sb);
}
