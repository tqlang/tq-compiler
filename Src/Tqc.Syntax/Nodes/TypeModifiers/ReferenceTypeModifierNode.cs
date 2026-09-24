using System.Text;

namespace Tqc.Syntax.Nodes.TypeModifiers;

public record ReferenceTypeModifierNode(
    TokenNode StarToken,
    ExpressionNode ValueType
) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(StarToken).AppendSyntaxNode(ValueType);
}
