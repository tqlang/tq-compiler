using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record ParameterNode(ExpressionNode Type, IdentifierExpressionNode Name) : SyntaxNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Type).AppendSyntaxNode(Name);
    public override SourceSpan Span => Type.Span + Name.Span;
}
