using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record LocalExpressionNode(
    TokenNode LetOrConstKeyword,
    ExpressionNode? Type,
    IdentifierExpressionNode Name
) : ExpressionNode
{
    public bool IsConst => LetOrConstKeyword.Value == "const";

    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .Append(LetOrConstKeyword).AppendSyntaxNode(Type).AppendSyntaxNode(Name);
    public override SourceSpan Span => LetOrConstKeyword.Span + Name.Span;
}
