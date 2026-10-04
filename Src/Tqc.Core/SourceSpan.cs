namespace Tqc.Core;

public record struct SourceSpan(int LineStart, int ColStart, int LineEnd, int ColEnd)
{
    public static SourceSpan operator +(SourceSpan a, SourceSpan b) => new SourceSpan(
        Math.Min(a.LineStart, b.LineStart),
        Math.Min(a.ColStart, b.ColStart),
        Math.Min(a.LineEnd, b.LineEnd),
        Math.Min(a.ColEnd, b.ColEnd)
    );
}
