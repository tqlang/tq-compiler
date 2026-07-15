using System.Text;

namespace Tq.Ast;

public class StringLiteralNode : ExpressionNode
{
    public TokenNode LeftQuote;
    public StringContent[] Contents;
    public TokenNode RightQuote;

    private StringLiteralNode(TokenNode leftQuote, StringContent[] content, TokenNode rightQuote)
    {
        LeftQuote = leftQuote;
        Contents = content;
        RightQuote = rightQuote;
    }

    public static StringLiteralNode Build(TokenNode leftQuote, StringContent[] content, TokenNode rightQuote)
        => new(leftQuote, content, rightQuote);

    public static TextStringContent BuildTextContent(TokenNode value) => new(value);
    public static CharStringContent BuildCharContent(TokenNode value) => new(value);
    public static InterpolationStringContent BuildInterpolationContent(TokenNode open, ExpressionNode expression, TokenNode close) => new(open, expression, close);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        LeftQuote.AppendStringBuilder(sb);
        foreach (var i in Contents) i.AppendStringBuilder(sb);
        RightQuote.AppendStringBuilder(sb);
        return sb;
    }
}

public abstract class StringContent : ExpressionNode;

public class TextStringContent : StringContent
{
    public TokenNode Value;
    internal TextStringContent(TokenNode value) => Value = value;
    public override StringBuilder AppendStringBuilder(StringBuilder sb) => Value.AppendStringBuilder(sb);
}

public class CharStringContent : StringContent
{
    public TokenNode Value;
    internal CharStringContent(TokenNode value) => Value = value;
    public override StringBuilder AppendStringBuilder(StringBuilder sb) => Value.AppendStringBuilder(sb);
}

public class InterpolationStringContent : StringContent
{
    TokenNode Start;
    ExpressionNode Expression;
    TokenNode End;

    internal InterpolationStringContent(TokenNode start, ExpressionNode expression, TokenNode end)
    {
        Start = start;
        Expression = expression;
        End = end;
    }

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        Start.AppendStringBuilder(sb);
        Expression.AppendStringBuilder(sb);
        End.AppendStringBuilder(sb);
        return sb;
    }
}
