using System.Text;

namespace Tqc.Syntax.Nodes;

public record IdentifierExpressionNode(TokenNode Token) : ExpressionNode
{
    public string Value => Token.Value!;
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => Token.AppendSyntaxToStringBuilder(sb);
}
