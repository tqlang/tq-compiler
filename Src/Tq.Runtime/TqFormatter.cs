using System;
using System.Text;

namespace Tq;

/// <summary>How a value becomes text in an interpolated string.</summary>
public sealed class TqFormatter : IFormatProvider, ICustomFormatter
{
    public static readonly TqFormatter Instance = new();

    /// <summary>What the compiled code calls for each part of an interpolated string.</summary>
    public static string Format(object? value) => Instance.Format(null, value, Instance);

    public object? GetFormat(Type? formatType) => formatType == typeof(ICustomFormatter) ? this : null;

    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
        switch (arg)
        {
            case null:
                return "";

            case bool boolean:
                return boolean ? "true" : "false";

            case Array array:
            {
                var text = new StringBuilder();
                text.Append('[');
                for (var i = 0; i < array.Length; i++)
                {
                    if (i != 0) text.Append(", ");
                    text.Append(Format(null, array.GetValue(i), this));
                }
                text.Append(']');
                return text.ToString();
            }

            case IFormattable formattable:
                return formattable.ToString(format, formatProvider);

            default:
                return arg.ToString() ?? "";
        }
    }
}
