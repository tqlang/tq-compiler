using System.Text;

namespace Tqc.Syntax.Nodes;

public record ElseStatementNode(
    TokenNode ElseKeyword,
    StatementNode Statement
) : StatementNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(ElseKeyword)
        .AppendSyntaxNode(Statement);
}
