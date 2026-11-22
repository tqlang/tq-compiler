using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

/// <summary>`implements A, B(T)`: the protocols a struct implements.</summary>
public record ImplementsClauseNode(TokenNode Keyword, (ExpressionNode node, TokenNode? comma)[] Protocols) : SyntaxNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb.AppendSyntaxNode(Keyword);
        foreach (var (node, comma) in Protocols) sb.AppendSyntaxNode(node).AppendSyntaxNode(comma);
        return sb;
    }

    public override SourceSpan Span => Keyword.Span + Protocols[^1].node.Span;
}
