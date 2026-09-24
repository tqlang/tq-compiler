using System.Text;

namespace Tqc.Syntax.Nodes;

public record LocalExpressionNode(
    TokenNode LetOrConstKeyword,
    ExpressionNode? Type,
    IdentifierExpressionNode Name
) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .Append(LetOrConstKeyword).AppendSyntaxNode(Type).AppendSyntaxNode(Name);
}
