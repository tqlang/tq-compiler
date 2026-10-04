using System.Text;
using Tqc.Core;

namespace Tqc.Syntax.Nodes;

public record TokenNode(
    TriviaNode[] Trivia,
    string Value,
    TokenKind Kind,
    int Line,
    int Column
) : SyntaxNode
{
    internal TokenNode(TriviaNode[] Trivia, TokenValue Value) : this(
        Trivia,
        Value.value,
        Value.kind,
        Value.Line,
        Value.Column
    ) { }

    override internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb)
    {
        foreach (var i in Trivia) i.AppendSyntaxToStringBuilder(sb);
        return sb.Append(Value);
    }
    
    public override SourceSpan Span => new SourceSpan(Line, Column, Line, Column + Value!.Length);
}
