using System.Text;

namespace Tq.Ast;

public abstract class StatementNode : SyntaxNode;

public class StatementBodyNode : StatementNode
{
    public readonly BodyNode Body;

    private StatementBodyNode(BodyNode body) => Body = body;
    public static StatementBodyNode Build(BodyNode body) => new(body);
    public override StringBuilder AppendStringBuilder(StringBuilder sb) => Body.AppendStringBuilder(sb);
}

public class StatementExpressionNode : StatementNode
{
    public readonly ExpressionNode Expression;

    private StatementExpressionNode(ExpressionNode exp) => Expression = exp;
    public static StatementExpressionNode Build(ExpressionNode exp) => new(exp);
    public override StringBuilder AppendStringBuilder(StringBuilder sb) => Expression.AppendStringBuilder(sb);
}


public class WhileStatementNode : StatementNode
{
    public TokenNode While;
    public ExpressionNode? Initialization;
    public TokenNode? Colon1;
    public ExpressionNode Condition;
    public TokenNode? Colon2;
    public ExpressionNode? Step;
    public StatementNode Action;

    private WhileStatementNode(
        TokenNode whileToken,
        ExpressionNode? initialization,
        TokenNode? colon1,
        ExpressionNode condition,
        TokenNode? colon2,
        ExpressionNode? step,
        StatementNode action
    )
    {
        While          = whileToken;
        Initialization = initialization;
        Colon1         = colon1;
        Condition      = condition;
        Colon2         = colon2;
        Step           = step;
        Action         = action;
    }

    public static WhileStatementNode BuildCondition(TokenNode whileToken, ExpressionNode condition, StatementNode action)
        => new(whileToken, null, null, condition, null, null, action);

    public static WhileStatementNode BuildConditionStep(TokenNode whileToken, ExpressionNode condition, TokenNode colon2, ExpressionNode step, StatementNode action)
        => new(whileToken, null, null, condition, colon2, step, action);

    public static WhileStatementNode BuildInitConditionStep(
        TokenNode whileToken,
        ExpressionNode initialization,
        TokenNode colon1,
        ExpressionNode condition,
        TokenNode colon2,
        ExpressionNode step,
        StatementNode action
    ) => new(whileToken, initialization, colon1, condition, colon2, step, action);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        While.AppendStringBuilder(sb);
        Initialization?.AppendStringBuilder(sb);
        Colon1?.AppendStringBuilder(sb);
        Condition?.AppendStringBuilder(sb);
        Colon2?.AppendStringBuilder(sb);
        Step?.AppendStringBuilder(sb);
        Action.AppendStringBuilder(sb);
        
        return sb;
    }
}

public class ForStatementNode : StatementNode
{
    public TokenNode For;
    public ExpressionNode Initialization;
    public TokenNode In;
    public ExpressionNode Expression;

    private ForStatementNode(TokenNode forToken, ExpressionNode initialization, TokenNode inToken, ExpressionNode expression)
    {
        For            = forToken;
        Initialization = initialization;
        In             = inToken;
        Expression     = expression;
    }

    public static ForStatementNode Build(TokenNode forToken, ExpressionNode initialization, TokenNode inToken, ExpressionNode expression)
        => new (forToken, initialization, inToken, expression);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        For.AppendStringBuilder(sb);
        Initialization.AppendStringBuilder(sb);
        In.AppendStringBuilder(sb);
        Expression.AppendStringBuilder(sb);
        return sb;
    }
}
