using System.Text;

namespace Tq.Ast;

public class IntegerLiteralNode: ExpressionNode
{
    public TokenNode Value;
    
    private IntegerLiteralNode(TokenNode value)
    {
        Value = value;
    }

    public static IntegerLiteralNode Build(TokenNode value) => new (value);

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => Value.AppendStringBuilder(sb);
}
