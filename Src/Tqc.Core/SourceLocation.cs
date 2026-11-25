namespace Tqc.Core;

public record struct SourceLocation(string filePath, SourceSpan span)
{
    public SourceLocation(
        string filePath,
        int lineBegin,
        int columnBegin,
        int lineEnd,
        int columnEnd) : this(filePath, new(lineBegin, columnBegin, lineEnd, columnEnd)) {}

    public override string ToString() => $"{filePath}({span.LineStart+1}:{span.ColStart+1})";
}
