using System.Text;

namespace Tqc.Syntax.Nodes;

public record CallExpressionNode(ExpressionNode Callee, ArgumentsCollectionNode Arguments) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Callee).AppendSyntaxNode(Arguments);
}
