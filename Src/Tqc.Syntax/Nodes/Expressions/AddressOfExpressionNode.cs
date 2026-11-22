using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

/// <summary>`&x`: a slice that is a reference to the array `x`.</summary>
public record AddressOfExpressionNode(TokenNode Ampersand, ExpressionNode Operand) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Ampersand).AppendSyntaxNode(Operand);
    public override SourceSpan Span => Ampersand.Span + Operand.Span;
}
