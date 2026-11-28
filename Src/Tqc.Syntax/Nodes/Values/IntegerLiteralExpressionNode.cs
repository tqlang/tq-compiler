using System.Globalization;
using System.Numerics;
using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record IntegerLiteralExpressionNode(TokenNode Token) : ExpressionNode
{
    public readonly BigInteger Value = ParseTokenValue(Token);
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => Token.AppendSyntaxToStringBuilder(sb);
    public override SourceSpan Span => Token.Span;

    private static BigInteger ParseTokenValue(TokenNode token)
    {
        var tokenVal = token.Value;
        
        // A literal has no sign: the `0` in front keeps `0xFF` from being read as the two's complement -1.
        if (tokenVal.StartsWith("0x")) return BigInteger.Parse("0" + NumberText.WithoutSeparators(tokenVal[2..]), NumberStyles.HexNumber);
        if (tokenVal.StartsWith("0b")) return BigInteger.Parse("0" + NumberText.WithoutSeparators(tokenVal[2..]), NumberStyles.BinaryNumber);
        
        return BigInteger.Parse(NumberText.WithoutSeparators(tokenVal));
    }
}
