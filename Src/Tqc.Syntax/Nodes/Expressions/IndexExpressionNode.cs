using System.Text;

namespace Tqc.Syntax.Nodes;

public record IndexExpressionNode(ExpressionNode Callee, IndexCollectionNode Indices) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Callee).AppendSyntaxNode(Indices);
}
