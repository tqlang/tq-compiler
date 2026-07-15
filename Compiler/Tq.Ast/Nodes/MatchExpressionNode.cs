using System.Text;

namespace Tq.Ast;

public class MatchExpressionNode : ExpressionNode
{
    public TokenNode Match;
    public ExpressionNode Expression;

    public TokenNode LeftBracket;
    public MatchBaseCaseNode[] Cases;
    public TokenNode RightBracket;

    public static MatchExpressionNode Build(
        TokenNode matchToken,
        ExpressionNode expression,
        TokenNode leftBracket,
        MatchBaseCaseNode[] cases,
        TokenNode rightBracket
    ) => new(matchToken, expression, leftBracket, cases, rightBracket);

    private MatchExpressionNode(
        TokenNode matchToken,
        ExpressionNode expression,
        TokenNode leftBracket,
        MatchBaseCaseNode[] cases,
        TokenNode rightBracket
    )
    {
        Match        = matchToken;
        Expression   = expression;
        LeftBracket  = leftBracket;
        Cases        = cases;
        RightBracket = rightBracket;
    }

    public static MatchValueCaseNode BuildValueCase(TokenNode caseToken, (ExpressionNode exp, TokenNode? comma)[] expressions, TokenNode arrow, StatementNode body)
        => new(caseToken, expressions, arrow, body);

    public static MatchDefaultCaseNode BuildDefaultCase(TokenNode defaultToken, TokenNode arrow, StatementNode body) => new(defaultToken, arrow, body);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        Match.AppendStringBuilder(sb);
        Expression.AppendStringBuilder(sb);

        LeftBracket.AppendStringBuilder(sb);
        foreach (var option in Cases) option.AppendStringBuilder(sb);
        RightBracket.AppendStringBuilder(sb);

        return sb;
    }
}

public abstract class MatchBaseCaseNode : SyntaxNode;

public class MatchValueCaseNode : MatchBaseCaseNode
{
    public TokenNode Case;
    public (ExpressionNode exp, TokenNode? comma)[] Expressions;
    public TokenNode Arrow;
    public StatementNode Body;

    internal MatchValueCaseNode(TokenNode caseNode, (ExpressionNode exp, TokenNode? comma)[] expressions, TokenNode arrow, StatementNode body)
    {
        Case        = caseNode;
        Expressions = expressions;
        Arrow       = arrow;
        Body        = body;
    }

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        Case.AppendStringBuilder(sb);

        foreach (var (exp, comma) in Expressions)
        {
            exp.AppendStringBuilder(sb);
            comma?.AppendStringBuilder(sb);
        }

        Arrow.AppendStringBuilder(sb);
        Body.AppendStringBuilder(sb);

        return sb;
    }
}

public class MatchDefaultCaseNode : MatchBaseCaseNode
{
    public TokenNode Default;
    public TokenNode Arrow;
    public StatementNode Body;

    internal MatchDefaultCaseNode(TokenNode defaultNode, TokenNode arrow, StatementNode body)
    {
        Default = defaultNode;
        Arrow   = arrow;
        Body    = body;
    }

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        Default.AppendStringBuilder(sb);
        Arrow.AppendStringBuilder(sb);
        Body.AppendStringBuilder(sb);

        return sb;
    }
}