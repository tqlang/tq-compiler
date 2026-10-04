using System.Text;
using Tqc.Core;
using Tqc.Syntax.Nodes;

namespace Tqc.Semantics;

internal static class Extensions
{
    
    public static SourceLocation GetLoc(this SyntaxNode sn, string sourceFile) => new (sourceFile, sn.Span);
    
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
