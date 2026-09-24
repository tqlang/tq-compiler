using System.Text;

namespace Tqc.Syntax.Nodes;

public record TokenNode : SyntaxNode
{
    private TriviaNode[] Trivia;
    public string? Value;
    public TokenKind Kind;
    public int Line;
    public int Column;
    
    internal TokenNode(TriviaNode[] Trivia, TokenValue Value)
    {
        this.Trivia = Trivia;
        this.Value = Value.value;
        Kind       = Value.kind;
        Line       = Value.Line;
        Column     = Value.Column;
    }

    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        foreach (var i in Trivia) i.AppendSyntaxToStringBuilder(sb);
        return sb.Append(Value);
    }
}
