using System.Text;

namespace Tqc.Syntax.Nodes;

public record UnaryPrefixExpressionNode( TokenNode Op, ExpressionNode Exp) : ExpressionNode
{
    public string OperatorValue => Op.Value!;
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Op).AppendSyntaxNode(Exp);
}
