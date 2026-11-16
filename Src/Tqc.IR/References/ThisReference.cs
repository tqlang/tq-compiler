using Tqc.IR.ProgramMembers;

namespace Tqc.IR.References;

/// <summary>`this`: the instance a member function of a struct or typedef runs on.</summary>
public sealed record ThisReference(IType Owner) : Ref
{
    public override string ToString() => $"this ({Owner.Name})";
}
