using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.IR.References;

public sealed record PropertyReference(TqProperty Property) : MemberRef(Property)
{
    public override string ToString() => Property.Name;
}
