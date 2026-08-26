namespace Tq.CodeProcess.Core.Language;

public static class CharExtensions
{
    private static readonly char[] _languageSymbols = [
        '=', '+', '-', '*', '/', '!', '@', '$', '%', '&', '|', ':', ';', '.', '?', '<', '>'
    ];

    extension(char c)
    {
        public bool IsValidOnIdentifier() => char.IsLetterOrDigit(c) || c == '_';
        public bool IsValidOnIdentifierStarter() => char.IsLetter(c) || c == '_';
        public bool IsLanguageSymbol() => _languageSymbols.Contains(c);
    }

}
