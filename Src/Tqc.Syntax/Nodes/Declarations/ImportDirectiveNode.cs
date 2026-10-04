using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record ImportDirectiveNode(
    TokenNode FromToken,
    CompoundIdentifierExpressionNode NamespaceIdentifier,
    TokenNode ImportToken,
    MembersCollectionNode? ImportMembers,
    TriviaNode[] LeadingTrivia
) : SyntaxNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(FromToken)
        .AppendSyntaxNode(NamespaceIdentifier)
        .AppendSyntaxNode(ImportToken)
        .AppendSyntaxNode(ImportMembers)
        .AppendTrivia(LeadingTrivia);

    public override SourceSpan Span => FromToken.Span + SourceSpan.FirstAndLastNotNull(ImportToken, ImportMembers);
}
