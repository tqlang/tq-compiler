using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes.TypeModifiers;

public record ReferenceTypeModifierNode(
    TokenNode StarToken,
    ExpressionNode ValueType
) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(StarToken).AppendSyntaxNode(ValueType);
    public override SourceSpan Span => StarToken.Span + ValueType.Span;
}
