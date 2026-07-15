using System.Text;

namespace Tq.Ast;

public class SliceModifierNode : ExpressionNode
{
    public readonly TokenNode LeftSquareBracketToken;
    public readonly TokenNode RightSquareBracketToken;
    public readonly ExpressionNode ElementType;
    
    private SliceModifierNode(
        TokenNode leftSquareBracketToken,
        TokenNode rightSquareBracketToken,
        ExpressionNode elementType
    )
    {
        ElementType                 = elementType;
        LeftSquareBracketToken  = leftSquareBracketToken;
        RightSquareBracketToken = rightSquareBracketToken;
    }

    public static SliceModifierNode Build(
        TokenNode leftSquareBracketToken,
        TokenNode rightSquareBracketToken,
        ExpressionNode subtype
    ) => new(leftSquareBracketToken, rightSquareBracketToken, subtype);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        LeftSquareBracketToken.AppendStringBuilder(sb);
        RightSquareBracketToken.AppendStringBuilder(sb);
        return ElementType.AppendStringBuilder(sb);
    }
}
