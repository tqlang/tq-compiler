using System.Text;

namespace Tqc.Syntax.Nodes;

public record DecimalLiteralExpressionNode(TokenNode Token) : ExpressionNode
{
    public readonly decimal Value = decimal.Parse(Token.Value!);
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Token);
}
