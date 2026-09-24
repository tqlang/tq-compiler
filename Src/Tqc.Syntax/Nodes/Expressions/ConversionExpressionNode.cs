using System.Text;

namespace Tqc.Syntax.Nodes;

public record ConversionExpressionNode(ExpressionNode Expression, TokenNode As, ExpressionNode Type) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Expression).AppendSyntaxNode(As).AppendSyntaxNode(Type);
}
