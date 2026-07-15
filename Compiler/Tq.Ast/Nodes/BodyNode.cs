using System.Text;

namespace Tq.Ast;

public abstract class BodyNode : SyntaxNode;

public class ExplicitBodyNode : BodyNode {
    public readonly TokenNode    LeftBraceToken;
    public readonly SyntaxNode[] Children;
    public readonly TokenNode    RightBraceToken;

    private ExplicitBodyNode(TokenNode leftBraceToken, SyntaxNode[] children, TokenNode rightBraceToken)
    {
        LeftBraceToken  = leftBraceToken;
        Children        = children;
        RightBraceToken = rightBraceToken;
    }

    public static ExplicitBodyNode Build(TokenNode leftBraceToken, SyntaxNode[] children, TokenNode rightBraceToken) => new (leftBraceToken, children, rightBraceToken);

    public override StringBuilder AppendStringBuilder(StringBuilder sb) => sb.Append(LeftBraceToken).AppendJoin("", Children).Append(RightBraceToken);
}
