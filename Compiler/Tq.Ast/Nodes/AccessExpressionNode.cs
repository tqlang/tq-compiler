using System.Text;

namespace Tq.Ast;

public class AccessExpressionNode : ExpressionNode
{
    public readonly ExpressionNode? Accessed;
    public readonly TokenNode Dot;
    public readonly IdentifierNode Access;

    private AccessExpressionNode(ExpressionNode? accessed, TokenNode dot, IdentifierNode access)
    {
        Accessed = accessed;
        Dot      = dot;
        Access   = access;
    }

    public static AccessExpressionNode Build(ExpressionNode accessed, TokenNode dot, IdentifierNode access) => new (accessed, dot, access);
    public static AccessExpressionNode BuildImplicit(TokenNode dot, IdentifierNode access) => new (null, dot, access);

    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        Accessed?.AppendStringBuilder(sb);
        Dot.AppendStringBuilder(sb);
        Access.AppendStringBuilder(sb);
        return sb;
    }
}
