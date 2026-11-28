namespace Tqc.Syntax.Nodes;

internal static class NumberText
{
    /// <summary>
    /// The text of a number without its separators (`1_000` is `1000`). A `_` goes between two digits: one that starts or ends the
    /// digits, or is next to the `.`, is a <see cref="FormatException"/>.
    /// </summary>
    public static string WithoutSeparators(string digits) =>
        digits.StartsWith('_') || digits.EndsWith('_') || digits.Contains("_.") || digits.Contains("._")
            ? throw new FormatException($"Misplaced '_' in '{digits}'")
            : digits.Replace("_", "");
}
