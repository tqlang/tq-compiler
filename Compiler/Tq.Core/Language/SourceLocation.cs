namespace Tq.Core.Language;

public record struct SourceLocation(
    string Script,
    int LineStart,
    int ColStart
)
{
    public override string ToString() => $"{Script}:{LineStart}:{ColStart}";
}