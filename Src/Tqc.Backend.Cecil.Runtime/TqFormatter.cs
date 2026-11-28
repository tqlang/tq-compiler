using System;
using System.Runtime.CompilerServices;

namespace Tq;

public sealed class TqFormatter : IFormatProvider, ICustomFormatter
{
    public static readonly TqFormatter Instance = new();
    
    public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;
    
    public static string Format(object? value) => Instance.Format(null, value, Instance);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Format(bool value) => value ? "true" : "false";
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Format(bool? value) => value.GetValueOrDefault() ? "true" : (value.HasValue ? "false" : string.Empty);

    public static string FormatValue<T>(T value)
    {
        if (typeof(T) == typeof(bool)) return Format(Unsafe.As<T, bool>(ref value));
        if (typeof(T) == typeof(bool?)) return Format(Unsafe.As<T, bool?>(ref value));
        
        if (value is IFormattable formattable)
            return formattable.ToString(null, Instance);

        return value?.ToString() ?? string.Empty;
    }

    public static string Format<T>(T[] array) => FormatSpan(new ReadOnlySpan<T>(array));
    public static string Format<T>(Span<T> span) => FormatSpan(span);
    public static string Format<T>(Memory<T> mem) => FormatSpan(mem.Span);
    public static string Format<T>(Slice<T> slice) => FormatSpan(slice.Span);
    public static string Format<T>(ReadOnlySpan<T> span) => FormatSpan(span);
    public static string Format<T>(ReadOnlyMemory<T> mem) => FormatSpan(mem.Span);

    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
        return arg switch
        {
            null         => string.Empty,
            bool boolean => Format(boolean),
            Array a      => FormatArray(a),
            IFormattable formattable => formattable.ToString(format, formatProvider),
            _                        => arg.ToString() ?? string.Empty,
        };
    }

    private static string FormatArray(Array array)
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
            string[] a  => FormatSpan<string?>(a),
            object[] a  => FormatSpan<object?>(a),
            _           => FormatArrayFallback(array),
        };
    }

    private static string FormatSpan<T>(ReadOnlySpan<T> span)
    {
        if (span.IsEmpty) return "[]";
        
        var literalLength = 2 + Math.Max(0, span.Length - 1) * 2;
        var text = new DefaultInterpolatedStringHandler(literalLength, span.Length);

        text.AppendLiteral("[");
        for (var i = 0; i < span.Length; i++)
        {
            if (i != 0) text.AppendLiteral(", ");
            Append(ref text, span[i]);
        }
        text.AppendLiteral("]");
        return text.ToStringAndClear();
    }

    private static string FormatArrayFallback(Array array)
    {
        if (array.Length == 0) return "[]";
        
        var literalLength = 2 + Math.Max(0, array.Length - 1) * 2;
        var text = new DefaultInterpolatedStringHandler(literalLength, array.Length);

        text.AppendLiteral("[");
        for (var i = 0; i < array.Length; i++)
        {
            if (i != 0) text.AppendLiteral(", ");
            Append(ref text, array.GetValue(i));
        }
        text.AppendLiteral("]");
        return text.ToStringAndClear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Append<T>(ref DefaultInterpolatedStringHandler text, T value)
    {
        if (typeof(T) == typeof(bool)) text.AppendLiteral(Format(Unsafe.As<T, bool>(ref value)));
        else if (typeof(T) == typeof(bool?)) text.AppendLiteral(Format(Unsafe.As<T, bool?>(ref value)));
        else text.AppendFormatted(value);
    }
}
