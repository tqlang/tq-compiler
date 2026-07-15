using System.Text;

namespace Tq.Ast;

public sealed class SyntaxTree(string sourceFile, SyntaxNode[] nodes) {
    public readonly string       SourceFile = sourceFile;
    public readonly SyntaxNode[] Children   = nodes;

    public override string ToString() => AppendStringBuilder(new StringBuilder(), null).ToString();
    public StringBuilder AppendStringBuilder(StringBuilder builder, string? format)
    {
        foreach (var i in Children) i.AppendStringBuilder(builder);
        return builder;
    }
}
