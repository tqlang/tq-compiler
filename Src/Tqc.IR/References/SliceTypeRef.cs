namespace Tqc.IR.References;

/// <summary>`[]T`</summary>
public sealed record SliceTypeRef(ITypeRef Element) : Ref, ITypeRef
{
    public override string ToString() => $"[]{Element}";
}
