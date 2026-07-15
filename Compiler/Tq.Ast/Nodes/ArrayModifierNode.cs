using System.Text;

namespace Tq.Ast;

public class ArrayModifierNode : ExpressionNode
{
    public readonly TokenNode LeftSquareBracketToken;
    public readonly (ExpressionNode expression, TokenNode? comma)[] Arguments;
    public readonly TokenNode RightSquareBracketToken;
    public readonly ExpressionNode Subtype;


    private ArrayModifierNode(
        TokenNode leftSquareBracketToken,
        (ExpressionNode expression, TokenNode? comma)[] args,
        TokenNode rightSquareBracketToken,
        ExpressionNode subtype
    )
    {
        Subtype                 = subtype;
        LeftSquareBracketToken  = leftSquareBracketToken;
        Arguments               = args;
        RightSquareBracketToken = rightSquareBracketToken;
    }

    public static ArrayModifierNode Build(
        TokenNode leftSquareBracketToken,
        (ExpressionNode expression, TokenNode? comma)[] args,
        TokenNode rightSquareBracketToken,
        ExpressionNode subtype
    ) => new(leftSquareBracketToken, args, rightSquareBracketToken, subtype);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        LeftSquareBracketToken.AppendStringBuilder(sb);

        foreach (var i in Arguments)
        {
            i.expression.AppendStringBuilder(sb);
            i.comma?.AppendStringBuilder(sb);
        }

        RightSquareBracketToken.AppendStringBuilder(sb);
        return Subtype.AppendStringBuilder(sb);
    }
}