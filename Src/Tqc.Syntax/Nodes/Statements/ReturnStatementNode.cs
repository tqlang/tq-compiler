using System.Text;

namespace Tqc.Syntax.Nodes;

public record ReturnStatementNode(
    TokenNode ReturnKeyword,
    ExpressionNode? Expression
) : StatementNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(ReturnKeyword)
        .AppendSyntaxNode(Expression);
}
