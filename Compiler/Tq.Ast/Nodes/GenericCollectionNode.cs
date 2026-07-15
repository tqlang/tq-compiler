using System.Text;

namespace Tq.Ast;

public class GenericCollectionNode : ExpressionNode
{
    public readonly TokenNode LeftSquareBracketToken;
    public readonly TokenNode RightSquareBracketToken;

    public readonly (ExpressionNode expression, TokenNode? comma)[] Values;

    private GenericCollectionNode(
        TokenNode leftSquareBracketToken,
        (ExpressionNode expression, TokenNode? comma)[] values,
        TokenNode rightSquareBracketToken
    )
    {
        LeftSquareBracketToken  = leftSquareBracketToken;
        Values                  = values;
        RightSquareBracketToken = rightSquareBracketToken;
    }

    public static GenericCollectionNode Build(
        TokenNode leftSquareBracketToken,
        (ExpressionNode expression, TokenNode? comma)[] values,
        TokenNode rightSquareBracketToken
    ) => new(leftSquareBracketToken, values, rightSquareBracketToken);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        LeftSquareBracketToken.AppendStringBuilder(sb);

        foreach (var i in Values)
        {
            i.expression.AppendStringBuilder(sb);
            i.comma?.AppendStringBuilder(sb);
        }

        return RightSquareBracketToken.AppendStringBuilder(sb);
    }
}