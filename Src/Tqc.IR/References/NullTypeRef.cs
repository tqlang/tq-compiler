namespace Tqc.IR.References;

/// <summary>The type of `null`: it only fits a `?T`, and takes that type where it is used.</summary>
public sealed record NullTypeRef : Ref, ITypeRef
{
    public override string ToString() => "null";
}
