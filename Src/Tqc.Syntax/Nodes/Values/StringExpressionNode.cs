using System.Collections.Immutable;
using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record StringExpressionNode(
    TokenNode LeftDoubleQuotes,
    StringContentNode[] Contents,
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
    
    public override SourceSpan Span => LeftDoubleQuotes.Span + RightDoubleQuotes.Span;
}

public abstract record StringContentNode : SyntaxNode;
public record StringCharacterLiteralNode(TokenNode Value) : StringContentNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Value);
    public char GetCharacterLiteralValue() => Value.Value switch
    {
        "\\'"  => '\'',
        "\\\"" => '"',
        "\\0"  => '\x00',
        "\\t"  => '\t',
        "\\e"  => '\x1B',
        "\\n"  => '\n',
        "\\r"  => '\r',
        _ => throw new NotImplementedException(),
    };
    public override SourceSpan Span => Value.Span;
}
public record StringLiteralNode(TokenNode Value) : StringContentNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.AppendSyntaxNode(Value);
    public override SourceSpan Span => Value.Span;
}
public record InterpolatedExpressionNode(TokenNode LeftEscapedBrace, ExpressionNode Expressison, TokenNode RightBrace) : StringContentNode
{
    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb
        .AppendSyntaxNode(LeftEscapedBrace)
        .AppendSyntaxNode(Expressison)
        .AppendSyntaxNode(RightBrace);
    
    public override SourceSpan Span => LeftEscapedBrace.Span + RightBrace.Span;
}