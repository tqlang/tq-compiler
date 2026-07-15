namespace Tq.Core.Language;

public record struct SourceLocation(
    string Script,
    int LineStart,
    int ColStart,
    int LineEnd,
    int ColEnd
)
{
    public override string ToString() => $"{Script}:{LineStart}:{ColStart}";
}
