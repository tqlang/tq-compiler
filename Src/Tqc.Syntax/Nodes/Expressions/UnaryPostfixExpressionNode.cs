using System.Text;

namespace Tqc.Syntax.Nodes;

public record UnaryPostfixExpressionNode(ExpressionNode Exp, TokenNode Op) : ExpressionNode
{
    public string OperatorValue => Op.Value!;
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Exp).AppendSyntaxNode(Op);
}
