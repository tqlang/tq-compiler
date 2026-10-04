using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record AttributeNode(
    TokenNode AtToken,
    IdentifierExpressionNode Identifier,
    ArgumentsCollectionNode? Arguments
) : SyntaxNode
{

    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(AtToken).AppendSyntaxNode(Identifier).AppendSyntaxNode(Arguments);
    public override SourceSpan Span => AtToken.Span + SourceSpan.FirstAndLastNotNull(Identifier, Arguments);
}
