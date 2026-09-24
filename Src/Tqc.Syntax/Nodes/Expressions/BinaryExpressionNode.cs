using System.Text;

namespace Tqc.Syntax.Nodes;

public record BinaryExpressionNode(ExpressionNode Left, TokenNode Op, ExpressionNode Right) : ExpressionNode
{
    public string OperatorValue => Op.Value!;
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Left).AppendSyntaxNode(Op).AppendSyntaxNode(Right);
}
