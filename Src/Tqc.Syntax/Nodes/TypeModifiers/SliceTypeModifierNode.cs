using System.Text;

namespace Tqc.Syntax.Nodes.TypeModifiers;

public record SliceTypeModifierNode(
    TokenNode LeftBracket,
    TokenNode RightBracket,
    ExpressionNode ElementType
) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(LeftBracket).AppendSyntaxNode(RightBracket).AppendSyntaxNode(ElementType);
}
