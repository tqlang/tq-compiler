using System.Text;

namespace Tqc.Syntax.Nodes;

public record TriviaNode : SyntaxNode
{
    public string? Value;
    public TokenKind Kind;
    public int Line;
    public int Column;
    
    internal TriviaNode(TokenValue value)
    {
        Value = value.value;
        Kind = value.kind;
        Line = value.Line;
        Column = value.Column;
    }

    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb) => sb.Append(Value ?? "");
}
