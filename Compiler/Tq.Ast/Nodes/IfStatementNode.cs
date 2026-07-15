using System.Text;

namespace Tq.Ast;

public class IfStatementNode : StatementNode
{
    public TokenNode If;
    public ExpressionNode Condition;
    public StatementNode Then;

    private IfStatementNode(TokenNode ifToken, ExpressionNode condition, StatementNode then)
    {
        If        = ifToken;
        Condition = condition;
        Then      = then;
    }

    public static IfStatementNode Build(TokenNode ifToken, ExpressionNode condition, StatementNode then)
        => new (ifToken, condition, then);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        If.AppendStringBuilder(sb);
        Condition.AppendStringBuilder(sb);
        Then.AppendStringBuilder(sb);
        return sb;
    }
}
public class ElifStatementNode : StatementNode
{
    public TokenNode Elif;
    public ExpressionNode Condition;
    public StatementNode Then;

    private ElifStatementNode(TokenNode elifToken, ExpressionNode condition, StatementNode then)
    {
        Elif      = elifToken;
        Condition = condition;
        Then      = then;
    }

    public static ElifStatementNode Build(TokenNode elifToken, ExpressionNode condition, StatementNode then)
        => new(elifToken, condition, then);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        Elif.AppendStringBuilder(sb);
        Condition.AppendStringBuilder(sb);
        Then.AppendStringBuilder(sb);
        return sb;
    }
}
public class ElseStatementNode : StatementNode
{
    public TokenNode Else;
    public StatementNode Then;

    private ElseStatementNode(TokenNode elseToken, StatementNode then)
    {
        Else = elseToken;
        Then = then;
    }

    public static ElseStatementNode Build(TokenNode elifToken, StatementNode then)
        => new(elifToken, then);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        Else.AppendStringBuilder(sb);
        Then.AppendStringBuilder(sb);
        return sb;
    }
}

public class ConditionalChainNode : StatementNode
{
    public IfStatementNode If;
    public ElifStatementNode[] Elifs;
    public ElseStatementNode? Else;

    private ConditionalChainNode(IfStatementNode @if, ElifStatementNode[] elifs, ElseStatementNode? @else)
    {
        If    = @if;
        Elifs = elifs;
        Else = @else;
    }

    public static ConditionalChainNode Build(IfStatementNode @if, ElifStatementNode[] elifs, ElseStatementNode? @else)
        => new (@if, elifs, @else);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        If.AppendStringBuilder(sb);
        foreach (var i in Elifs) i.AppendStringBuilder(sb);
        Else?.AppendStringBuilder(sb);

        return sb;
    }
}