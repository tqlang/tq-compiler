using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes.TypeModifiers;

public record FailableTypeModifierNode(
    TokenNode BangToken,
    ExpressionNode ValueType
) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(BangToken).AppendSyntaxNode(ValueType);
    public override SourceSpan Span => BangToken.Span + ValueType.Span;
}
