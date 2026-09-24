using System.Collections.Immutable;
using System.Text;

namespace Tqc.Syntax.Nodes;

public record CompoundIdentifierExpressionNode(ImmutableArray<(TokenNode token, TokenNode? dot)> Identifier) : ExpressionNode
{
    public string[] Value => Identifier.Select(e => e.token.Value!).ToArray();
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        foreach (var (ident, dot) in Identifier)
        {
            sb.AppendSyntaxNode(ident);
            if (dot != null) sb.AppendSyntaxNode(dot);
        }
        return sb;
    }
}
