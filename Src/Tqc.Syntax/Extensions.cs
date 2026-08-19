using System.Text;

namespace Tqc.Syntax;

internal static class Extensions
{
    private static readonly char[] _languageSymbols = [
        '=', '+', '-', '*', '/', '!', '@', '$', '%', '&', '|', ':', ';', '.', '?', '<', '>',
    ];
    
    extension(char c)
    {
        public bool IsValidOnIdentifier() => char.IsLetterOrDigit(c) || c == '_';
        public bool IsValidOnIdentifierStarter() => char.IsLetter(c) || c == '_';
        public bool IsLanguageSymbol() => _languageSymbols.Contains(c);
    }

    public static string TabAll(this string? str)
    {
        if (str == null) return "<nil>";
        var sb = new StringBuilder();
        var lines = str.Split(Environment.NewLine);
        foreach (var l in lines)
        {
            if (string.IsNullOrEmpty(l)) sb.AppendLine();
            else sb.AppendLine($"\t{l}");
        }

        if (lines.Length > 0) sb.Length -= Environment.NewLine.Length;
        return sb.ToString();
    }
}
