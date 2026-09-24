using System.Collections.Immutable;
using System.Text;

namespace Tqc.Syntax.Nodes;

public record StringExpressionNode(
    TokenNode LeftDoubleQuotes,
    ImmutableArray<StringContentNode> Contents,
    TokenNode RightDoubleQuotes
) : ExpressionNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb.AppendSyntaxNode(LeftDoubleQuotes);
        foreach (var i in Contents) sb.AppendSyntaxNode(i);
        sb.AppendSyntaxNode(RightDoubleQuotes);
        return sb;
    }
}

public abstract record StringContentNode : SyntaxNode;
public record StringCharacterLiteralNode(TokenNode Value) : StringContentNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Value);
}
public record StringLiteralNode(TokenNode Value) : StringContentNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Value);
}
public record InterpolatedExpressionNode(TokenNode LeftEscapedBrace, ExpressionNode Expresison, TokenNode RightBrace) : StringContentNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(LeftEscapedBrace)
        .AppendSyntaxNode(Expresison)
        .AppendSyntaxNode(RightBrace);
}