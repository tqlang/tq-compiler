using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes.TypeModifiers;

/// <summary>`[5]T`: an array of a size known when compiling.</summary>
public record ArrayTypeModifierNode(
    TokenNode LeftBracket,
    ExpressionNode Length,
    TokenNode RightBracket,
    ExpressionNode ElementType
) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(LeftBracket).AppendSyntaxNode(Length).AppendSyntaxNode(RightBracket).AppendSyntaxNode(ElementType);
    public override SourceSpan Span => LeftBracket.Span + ElementType.Span;
}
