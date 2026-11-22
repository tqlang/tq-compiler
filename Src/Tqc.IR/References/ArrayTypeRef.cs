namespace Tqc.IR.References;

/// <summary>`[N]T`: memory with room for `Length` elements, which is known when compiling.</summary>
public sealed record ArrayTypeRef(int Length, ITypeRef Element) : Ref, ITypeRef
{
    public override string ToString() => $"[{Length}]{Element}";
}
