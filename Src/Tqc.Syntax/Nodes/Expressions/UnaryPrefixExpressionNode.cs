using System.Linq.Expressions;
using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record UnaryPrefixExpressionNode( TokenNode Op, ExpressionNode Exp) : ExpressionNode
{
    public string OperatorValue => Op.Value!;
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Op).AppendSyntaxNode(Exp);
    public override SourceSpan Span => Op.Span + Exp.Span;
}
