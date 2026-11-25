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
    /// <summary>
    /// The strings written on the lines after this one, with nothing but trivia between them: all of them are one string, with a
    /// line feed between each two. They have no `Following` of their own.
    /// </summary>
    public StringExpressionNode[] Following { get; init; } = [];

    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        sb.AppendSyntaxNode(LeftDoubleQuotes);
        foreach (var i in Contents) sb.AppendSyntaxNode(i);
        sb.AppendSyntaxNode(RightDoubleQuotes);
        foreach (var line in Following) line.AppendSyntaxToStringBuilder(sb);
        return sb;
    }
    
    public override SourceSpan Span => LeftDoubleQuotes.Span + (Following.Length > 0 ? Following[^1].RightDoubleQuotes.Span : RightDoubleQuotes.Span);
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