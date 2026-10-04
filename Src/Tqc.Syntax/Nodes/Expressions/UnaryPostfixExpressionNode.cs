using System.Linq.Expressions;
using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record UnaryPostfixExpressionNode(ExpressionNode Exp, TokenNode Op) : ExpressionNode
{
    public string OperatorValue => Op.Value!;
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Exp).AppendSyntaxNode(Op);
    public override SourceSpan Span => Exp.Span + Op.Span;
}
