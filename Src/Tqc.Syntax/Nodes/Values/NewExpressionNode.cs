using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record NewExpressionNode(
    TokenNode NewKeyword,
    ExpressionNode Type,
    ArgumentsCollectionNode Arguments
) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .Append(NewKeyword).AppendSyntaxNode(Type).AppendSyntaxNode(Arguments);
    public override SourceSpan Span => NewKeyword.Span + Arguments.Span;
}
