using System.Text;

namespace Tq.Ast;

public class NewObjectNode : ExpressionNode
{
    public TokenNode NewToken;
    public ExpressionNode Type;
    
    public TokenNode LeftParenthesisToken;
    public TokenNode RightParenthesisToken;

    public ExplicitBodyNode? Initializers;
    
    public readonly (ExpressionNode expression, TokenNode? comma)[] Arguments = [];

    private NewObjectNode(
        TokenNode newToken,
        ExpressionNode type,
        TokenNode leftP,
        (ExpressionNode expression, TokenNode? comma)[] args,
        TokenNode rightP,
        ExplicitBodyNode? initializers
    )
    {
        NewToken              = newToken;
        Type                  = type;
        LeftParenthesisToken  = leftP;
        Arguments             = args;
        RightParenthesisToken = rightP;
        initializers          = initializers;
    }
    
    public static NewObjectNode Build(
        TokenNode newToken,
        ExpressionNode type,
        TokenNode leftP,
        (ExpressionNode expression, TokenNode? comma)[] args,
        TokenNode rightP,
        ExplicitBodyNode? initializers
    ) => new NewObjectNode(newToken, type, leftP, args, rightP, initializers);
    
    public override StringBuilder AppendStringBuilder(StringBuilder sb)
    {
        sb.Append(NewToken)
          .Append(Type)
          .Append(LeftParenthesisToken);
        foreach (var i in Arguments)
        {
            sb.Append(i.expression);
            if (i.comma != null) sb.Append(i.comma);
        }
        return sb.Append(RightParenthesisToken);
    }
}
