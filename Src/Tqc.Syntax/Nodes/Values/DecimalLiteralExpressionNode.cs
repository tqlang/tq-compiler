using System.Globalization;
using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record DecimalLiteralExpressionNode(TokenNode Token) : ExpressionNode
{
    public readonly decimal Value = decimal.Parse(Token.Value!, CultureInfo.InvariantCulture);
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Token);
    public override SourceSpan Span => Token.Span;
}
