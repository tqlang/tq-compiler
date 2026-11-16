using Tqc.IR.ProgramMembers;

namespace Tqc.IR.References;

public record TypeMetaReference(IType Type) : Ref, IBuiltinTypeRef
{
    public override string ToString() => Type.Name;
}
