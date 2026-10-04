using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes.TypeModifiers;

public record SliceTypeModifierNode(
    TokenNode LeftBracket,
    TokenNode RightBracket,
    ExpressionNode ElementType
) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(LeftBracket).AppendSyntaxNode(RightBracket).AppendSyntaxNode(ElementType);
    public override SourceSpan Span => LeftBracket.Span + ElementType.Span;
}
