using System.Numerics;
using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record IntegerLiteralExpressionNode(TokenNode Token) : ExpressionNode
{
    public readonly BigInteger Value = BigInteger.Parse(Token.Value!);
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => Token.AppendSyntaxToStringBuilder(sb);
    public override SourceSpan Span => Token.Span;
}
