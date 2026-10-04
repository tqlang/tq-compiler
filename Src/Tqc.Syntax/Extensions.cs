using System.Globalization;
using System.Text;
using Tqc.Core;
using Tqc.Syntax.Nodes;

namespace Tqc.Syntax;

internal static class Extensions
{
    private static readonly char[] _languageSymbols =
    [
        '=', '+', '-', '*', '/', '!', '@', '$', '%', '&', '|', ':', ';', '.', '?', '<', '>',
    ];

    extension(Rune rune)
    {
        public bool IsIdentifierStart()
        {
            if (rune.Value == '_') return true;
            return Rune.GetUnicodeCategory(rune) switch
            {
                UnicodeCategory.UppercaseLetter => true,
                UnicodeCategory.LowercaseLetter => true,
                UnicodeCategory.TitlecaseLetter => true,
                UnicodeCategory.ModifierLetter  => true,
                UnicodeCategory.OtherLetter     => true,
                UnicodeCategory.LetterNumber    => true,

                _ => false,
            };

        }
        public bool IsIdentifierPart() => rune.IsIdentifierStart()
            || Rune.GetUnicodeCategory(rune) switch
            {
                UnicodeCategory.DecimalDigitNumber   => true,
                UnicodeCategory.NonSpacingMark       => true,
                UnicodeCategory.SpacingCombiningMark => true,
                UnicodeCategory.ConnectorPunctuation => true,
                _                                    => false,
            };

        public bool IsBinaryDigit() => rune.Value switch
        {
            '0' or '1' => true,
            _          => false,
        };
        public bool IsOctalDigit() => rune.Value switch
        {
            >= '0' and <= '7' => true,
            _                 => false,
        };
        public bool IsDecimalDigit() => rune.Value switch
        {
            >= '0' and <= '9' => true,
            _                 => false,
        };
        public bool IsHexDigit() => rune.Value switch
        {
            >= '0' and <= '9'
                or >= 'a' and <= 'f'
                or >= 'A' and <= 'F' => true,
            _ => false,
        };

        public Rune ToLowerInvariant() => Rune.ToLowerInvariant(rune);
    }

    extension(StringBuilder sb)
    {
        public StringBuilder AppendAttributesSyntaxNode(AttributeNode[] attributeNodes)
        {
            foreach (var attr in attributeNodes)
                sb.AppendSyntaxNode(attr);
            return sb;
        }

        public StringBuilder AppendSyntaxNode(SyntaxNode? node)
        {
            if (node == null) return sb;
            node.AppendSyntaxToStringBuilder(sb);
            return sb;
        }
        public StringBuilder AppendTrivia(params TriviaNode[] nodes)
        {
            foreach (var node in nodes) node.AppendSyntaxToStringBuilder(sb);
            return sb;
        }
    }

    extension(SourceSpan sc) {
        public static SourceSpan FirstNotNull(params SyntaxNode?[] nodes)
        {
            var n = nodes.LastOrDefault(e => e != null);
            return n?.Span ?? throw new NullReferenceException("All nodes are null");
        }
        
        public static SourceSpan LastNotNull(params SyntaxNode?[] nodes)
        {
            var n = nodes.LastOrDefault(e => e != null);
            return n?.Span ?? throw new NullReferenceException("All nodes are null");
        }
        
        public static SourceSpan FirstAndLastNotNull(params SyntaxNode?[] nodes)
        {
            var n1 = nodes.LastOrDefault(e => e != null);
            if (n1 == null) throw new NullReferenceException("All nodes are null");
            
            var n2 = nodes.LastOrDefault(e => e != null);
            if (n2 != null) return n1.Span + n2.Span;
            
            return n1.Span;
        }
    }

    public static T[] Dump<T>(this List<T> list)
    {
        var array = list.ToArray();
        list.Clear();
        return array;
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
