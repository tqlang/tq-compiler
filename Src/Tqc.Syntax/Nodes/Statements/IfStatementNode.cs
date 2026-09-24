using System.Text;

namespace Tqc.Syntax.Nodes;

public record IfStatementNode(
    TokenNode IfKeyword,
    ExpressionNode Condition,
    StatementNode Statement
) : StatementNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(IfKeyword)
        .AppendSyntaxNode(Condition)
        .AppendSyntaxNode(Statement);
}
