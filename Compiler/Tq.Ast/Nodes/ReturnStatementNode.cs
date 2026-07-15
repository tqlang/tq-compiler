using System.Text;

namespace Tq.Ast;

public class ReturnStatementNode : StatementNode
{
    public readonly TokenNode ReturnToken;
    public readonly ExpressionNode? Expression;
    
    private ReturnStatementNode(TokenNode returnToken, ExpressionNode? expression)
    {
        ReturnToken = returnToken;
        Expression  = expression;
    }

    public static ReturnStatementNode Build(TokenNode returnToken, ExpressionNode? expression) => new (returnToken, expression);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        ReturnToken.AppendStringBuilder(sb);
        Expression?.AppendStringBuilder(sb);
        return sb;
    }
}
