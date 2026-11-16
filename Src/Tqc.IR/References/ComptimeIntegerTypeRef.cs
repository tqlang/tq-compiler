namespace Tqc.IR.References;

/// <summary>The type of an integer literal: it has no size until it is used where a type is expected.</summary>
public sealed record ComptimeIntegerTypeRef : Ref, ITypeRef
{
    public override string ToString() => "comptime_int";
}
