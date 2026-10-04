using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record CallExpressionNode(ExpressionNode Callee, ArgumentsCollectionNode Arguments) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Callee).AppendSyntaxNode(Arguments);

    public override SourceSpan Span => Callee.Span + Arguments.Span;
}
