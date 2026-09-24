using System.Text;

namespace Tqc.Syntax.Nodes;

public record ExpressionStatementNode(ExpressionNode Node) : StatementNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Node);
}
