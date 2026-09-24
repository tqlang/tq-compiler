using System.Text;

namespace Tqc.Syntax.Nodes;

public record ImportDefinitionNode(
    TokenNode FromToken,
    CompoundIdentifierExpressionNode NamespaceIdentifier,
    TokenNode ImportToken,
    MembersCollectionNode? ImportMembers
) : SyntaxNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(FromToken)
        .AppendSyntaxNode(NamespaceIdentifier)
        .AppendSyntaxNode(ImportToken)
        .AppendSyntaxNode(ImportMembers);
}
