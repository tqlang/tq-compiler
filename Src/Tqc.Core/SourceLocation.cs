namespace Tqc.Core;

public record struct SourceLocation(string filePath, SourceSpan span)
{
    public SourceLocation(
        string filePath,
        int lineBegin,
        int columnBegin,
        int lineEnd,
        int columnEnd) : this(filePath, new(lineBegin, columnBegin, lineEnd, columnEnd)) {}
}
