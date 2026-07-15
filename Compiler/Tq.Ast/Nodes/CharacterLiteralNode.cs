using System.Text;

namespace Tq.Ast;

public class CharacterLiteralNode: ExpressionNode
{
    public TokenNode LeftTick;
    public TokenNode Value;
    public TokenNode RightTick;
    
    private CharacterLiteralNode(TokenNode left, TokenNode value, TokenNode right) => (LeftTick, Value, RightTick) = (left, value, right);

    public static CharacterLiteralNode Build(TokenNode left, TokenNode value, TokenNode right) => new (left, value, right);

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => Value.AppendStringBuilder(sb);
}
