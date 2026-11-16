using Tqc.IR.ProgramMembers;

namespace Tqc.IR.References;

public sealed record FieldReference(IField Field) : MemberRef(Field)
{
    public override string ToString() => Field.Name;
}
