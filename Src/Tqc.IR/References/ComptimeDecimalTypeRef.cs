namespace Tqc.IR.References;

/// <summary>The type of a decimal literal: it is an `f64` unless it is used where another float is expected.</summary>
public sealed record ComptimeDecimalTypeRef : Ref, ITypeRef
{
    public override string ToString() => "comptime_decimal";
}
