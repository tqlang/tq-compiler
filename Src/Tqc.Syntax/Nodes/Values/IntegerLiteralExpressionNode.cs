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
        
        if (tokenVal.StartsWith("0x")) return BigInteger.Parse(tokenVal[2..], NumberStyles.HexNumber);
        if (tokenVal.StartsWith("0b")) return BigInteger.Parse(tokenVal[2..], NumberStyles.BinaryNumber);
        
        return BigInteger.Parse(tokenVal);
    }
}
