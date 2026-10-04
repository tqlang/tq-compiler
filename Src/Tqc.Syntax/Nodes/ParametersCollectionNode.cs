using System.Collections;
using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record ParametersCollectionNode(
    TokenNode LeftParenthesis,
    (ParameterNode node, TokenNode? comma)[] Parameters,
    TokenNode RightParenthesis
) : SyntaxNode, IEnumerable<ParameterNode>
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb.AppendSyntaxNode(LeftParenthesis);
        foreach (var (node, comma) in Parameters)
        {
            sb.Append(node);
            if (comma != null) sb.Append(comma);
        }
        sb.AppendSyntaxNode(RightParenthesis);
        return sb;
    }
    
    public override SourceSpan Span => LeftParenthesis.Span + RightParenthesis.Span;
    
    public IEnumerator<ParameterNode> GetEnumerator() 
    {
        foreach (var (node, _) in Parameters) yield return node;
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
