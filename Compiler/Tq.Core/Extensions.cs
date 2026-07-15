using System.Text;

namespace Tq.Core;

public static class Extensions
{
    extension(char c) {
        public bool IsValidOnIdentifierStarter() => c is '_' or >= 'a' and <= 'z' or >= 'A' and <= 'Z';
        public bool IsValidOnIdentifier() => c is '_' or >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';
    }

    extension(string? str) {
        public string TabAll(int tabs = 1)
        {
            if (str == null) return "<nil>";
            var sb    = new StringBuilder();
            var lines = str.Split(Environment.NewLine);
            foreach (var l in lines)
            {
                if (string.IsNullOrEmpty(l)) sb.AppendLine();
                else
                {
                    sb.Append('\t', tabs);
                    sb.AppendLine($"{l}");
                }
            }

            if (lines.Length > 0) sb.Length -= Environment.NewLine.Length;
            return sb.ToString();
        }
    }
    
    public static T[] Dump<T>(this List<T> list)
    {
        var arr = list.ToArray();
        list.Clear();
        return arr;
    }
}
