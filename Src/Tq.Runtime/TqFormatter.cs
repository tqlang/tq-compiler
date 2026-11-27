using System;
using System.Text;

namespace Tq;

public sealed class TqFormatter : IFormatProvider, ICustomFormatter
{
    public static readonly TqFormatter Instance = new();
    public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;
    
    
    public static string Format(object? value) => Instance.Format(null, value, Instance);
    public static string Format<T>(T[] array) => Instance.FormatSpan(array.AsSpan());
    public static string Format<T>(Span<T> span) => Instance.FormatSpan(span);
    public static string Format<T>(Memory<T> mem) => Instance.FormatSpan(mem.Span);
    public static string Format<T>(ReadOnlySpan<T> span) => Instance.FormatSpan(span);
    public static string Format<T>(ReadOnlyMemory<T> mem) => Instance.FormatSpan(mem.Span);
    
    
    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
        return arg switch
        {
            null         => "",
            bool boolean => boolean ? "true" : "false",
            Array a      => FormatArray(a),

            IFormattable formattable => formattable.ToString(format, formatProvider),
            _                        => arg.ToString() ?? "",
        };
    }

    
    private string FormatArray(Array array)
    {
        return array switch
        {
            bool[] a    => FormatSpan(a),
            byte[] a    => FormatSpan(a),
            sbyte[] a   => FormatSpan(a),
            short[] a   => FormatSpan(a),
            ushort[] a  => FormatSpan(a),
            int[] a     => FormatSpan(a),
            uint[] a    => FormatSpan(a),
            long[] a    => FormatSpan(a),
            ulong[] a   => FormatSpan(a),
            char[] a    => FormatSpan(a),
            float[] a   => FormatSpan(a),
            double[] a  => FormatSpan(a),
            decimal[] a => FormatSpan(a),
            string[] a  => FormatSpan<string?>(a!),
            object[] a  => FormatSpan<object?>(a!),
            _           => FormatArrayFallback(array),
        };
    }
    private string FormatSpan<T>(ReadOnlySpan<T> span)
    {
        var text = new StringBuilder();

        text.Append('[');
        for (var i = 0; i < span.Length; i++)
        {
            if (i != 0) text.Append(", ");
            text.Append(Format(null, span[i], this));
        }
        return text.Append(']').ToString();
    }
    private string FormatArrayFallback(Array array)
    {
        var text = new StringBuilder("[");
        var enumerator = array.GetEnumerator();

        while (enumerator.MoveNext())
        {
            if (text.Length > 1) text.Append(", ");
            text.Append(Format(null, enumerator.Current, this));
        }

        return text.Append(']').ToString();
    }
}
