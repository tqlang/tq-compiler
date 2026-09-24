using System.Text;

namespace Tqc.Syntax.Nodes;

public abstract record SyntaxNode : IFormattable
{

    public string ToString(string? format, IFormatProvider? formatProvider) => format switch
    {
        _ => AppendSyntaxToStringBuilder(new StringBuilder()).ToString(),
    };
    public override sealed string ToString() => ToString(null, null);

    abstract internal StringBuilder AppendSyntaxToStringBuilder(StringBuilder sb);
}
