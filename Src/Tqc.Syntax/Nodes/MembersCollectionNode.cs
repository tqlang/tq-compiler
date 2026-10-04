using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record MembersCollectionNode(
    TokenNode LeftBrace,
    (MembersCollectionItemNode node, TokenNode? comma)[] Identifiers,
    TokenNode RightBrace
) : SyntaxNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb.AppendSyntaxNode(LeftBrace);
        foreach (var (node, comma) in Identifiers)
        {
            sb.AppendSyntaxNode(node);
            if (comma != null) sb.AppendSyntaxNode(comma);
        }
        sb.AppendSyntaxNode(RightBrace);
        return sb;
    }
    public override SourceSpan Span => LeftBrace.Span + RightBrace.Span;
}

public abstract record MembersCollectionItemNode : SyntaxNode;
public record MembersCollectionPureItemNode(IdentifierExpressionNode value) : MembersCollectionItemNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(value);
    public override SourceSpan Span => value.Span;
}
public record MembersCollectionAliasedItemNode(IdentifierExpressionNode value, TokenNode asKeyword, IdentifierExpressionNode alias) : MembersCollectionItemNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(value).AppendSyntaxNode(asKeyword).AppendSyntaxNode(alias);
    public override SourceSpan Span => value.Span + alias.Span;
}