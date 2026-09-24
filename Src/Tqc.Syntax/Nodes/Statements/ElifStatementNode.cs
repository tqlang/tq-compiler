using System.Text;

namespace Tqc.Syntax.Nodes;

public record ElifStatementNode(
    TokenNode ElifKeyword,
    ExpressionNode Condition,
    StatementNode Statement
) : StatementNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(ElifKeyword)
        .AppendSyntaxNode(Condition)
        .AppendSyntaxNode(Statement);
}
