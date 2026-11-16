using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.IR.References;

/// <summary>The type a typedef declares.</summary>
public sealed record TypedefTypeReference(TqTypedef Typedef) : Ref, ITypeRef
{
    public override string ToString() => Typedef.Name;
}
