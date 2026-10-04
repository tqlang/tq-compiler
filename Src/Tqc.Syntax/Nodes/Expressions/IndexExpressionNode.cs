using System.Linq.Expressions;
using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record IndexExpressionNode(ExpressionNode Callee, IndexCollectionNode Indices) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Callee).AppendSyntaxNode(Indices);
    public override SourceSpan Span => Callee.Span + Indices.Span;
}
