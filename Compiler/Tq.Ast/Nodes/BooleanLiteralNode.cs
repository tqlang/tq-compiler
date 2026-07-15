using System.Text;

namespace Tq.Ast;

public class BooleanLiteralNode: ExpressionNode
{
    public TokenNode Value;
    
    private BooleanLiteralNode(TokenNode value)
    {
        Value = value;
    }

    public static BooleanLiteralNode Build(TokenNode value) => new (value);

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => Value.AppendStringBuilder(sb);
}
