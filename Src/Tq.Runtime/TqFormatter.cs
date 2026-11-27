using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tq;

public sealed class TqFormatter : IFormatProvider, ICustomFormatter
{
    public static readonly TqFormatter Instance = new();
    public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;
    
    
    public static string Format(object? value) => Instance.Format(null, value, Instance);
    public static string Format(bool value) => value ? "true" : "false";
    public static string Format(bool? value) => value is { } boolean ? Format(boolean) : "";

    /// <summary>A value of a type parameter: nothing is boxed when it turns out to be a value.</summary>
    public static string FormatValue<T>(T value)
    {
        var text = new DefaultInterpolatedStringHandler(0, 1);
        Append(ref text, value);
        return text.ToStringAndClear();
    }

    public static string Format<T>(T[] array) => FormatSpan<T>(array);
    public static string Format<T>(Span<T> span) => FormatSpan<T>(span);
    public static string Format<T>(Memory<T> mem) => FormatSpan<T>(mem.Span);
    public static string Format<T>(ReadOnlySpan<T> span) => FormatSpan(span);
    public static string Format<T>(ReadOnlyMemory<T> mem) => FormatSpan(mem.Span);
    
    
    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
        return arg switch
        {
            null         => "",
            bool boolean => Format(boolean),
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
    private static string FormatSpan<T>(ReadOnlySpan<T> span)
    {
        var text = new DefaultInterpolatedStringHandler(2, span.Length);

        text.AppendLiteral("[");
        for (var i = 0; i < span.Length; i++)
        {
            if (i != 0) text.AppendLiteral(", ");
            Append(ref text, span[i]);
        }
        text.AppendLiteral("]");
        return text.ToStringAndClear();
    }

    /// <summary>The checks of `typeof(T)` are resolved by the JIT, which then does not box the value.</summary>
    private static void Append<T>(ref DefaultInterpolatedStringHandler text, T value)
    {
        if (typeof(T) == typeof(bool)) text.AppendLiteral(Format((bool)(object)value!));
        else if (typeof(T) == typeof(bool?)) text.AppendLiteral(Format((bool?)(object?)value));
        else if (typeof(T).IsValueType) text.AppendFormatted(value);
        else text.AppendLiteral(Format((object?)value));
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
