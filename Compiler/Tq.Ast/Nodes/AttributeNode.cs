using System.Text;

namespace Tq.Ast;

public abstract class AttributeNode : SyntaxNode {
    public static SimpleAttributeNode BuildSimple(TokenNode at, IdentifierNode identifier) => new (at, identifier);

    public static CompleteAttributeNode BuildComplete(
        TokenNode at,
        IdentifierNode identifier,
        TokenNode leftParenthesisToken,
        (ExpressionNode expression, TokenNode? comma)[] args,
        TokenNode rightParenthesisToken
    ) =>  new (at, identifier, leftParenthesisToken, args, rightParenthesisToken);
}

public class SimpleAttributeNode : AttributeNode {
    public readonly TokenNode      At;
    public readonly IdentifierNode Identifier;

    internal SimpleAttributeNode(TokenNode at, IdentifierNode identifier)
    {
        At         = at;
        Identifier = identifier;
    }

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => sb.Append(At).Append(Identifier);
}

public class CompleteAttributeNode : AttributeNode {
    public readonly TokenNode                                       At;
    public readonly IdentifierNode                                  Identifier;
    public readonly TokenNode                                       LeftParenthesisToken;
    public readonly TokenNode                                       RightParenthesisToken;
    public readonly (ExpressionNode expression, TokenNode? comma)[] Arguments;

    internal CompleteAttributeNode(
        TokenNode at,
        IdentifierNode identifier,
        TokenNode leftParenthesisToken,
        (ExpressionNode expression, TokenNode? comma)[] args,
        TokenNode rightParenthesisToken
    )
    {
        At                    = at;
        Identifier            = identifier;
        LeftParenthesisToken  = leftParenthesisToken;
        RightParenthesisToken = rightParenthesisToken;
        Arguments             = args;
        RightParenthesisToken = rightParenthesisToken;
    }

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => sb
        .Append(At)
        .Append(Identifier)
        .Append(LeftParenthesisToken)
        .AppendJoin("", Arguments)
        .Append(RightParenthesisToken);
}
