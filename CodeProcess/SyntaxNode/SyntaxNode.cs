using CodeProcess.Lexing;

namespace CodeProcess.SyntaxNode;

public abstract class SyntaxNode
{
    public virtual Token[] Tokens { get; }

    public SyntaxNodeSpan Span
    {
        get
        {
            var f = Tokens[0];
            var l = Tokens[^1];
            return new SyntaxNodeSpan(f.Line, f.Column, l.Line, l.Column + l.Length);
        }
    }
}
