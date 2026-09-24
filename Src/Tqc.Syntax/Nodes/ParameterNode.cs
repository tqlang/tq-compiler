using System.Text;

namespace Tqc.Syntax.Nodes;

public record ParameterNode(ExpressionNode Type, IdentifierExpressionNode Name) : SyntaxNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Type).AppendSyntaxNode(Name);
}
