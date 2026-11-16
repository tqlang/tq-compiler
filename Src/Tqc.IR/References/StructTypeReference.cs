using Tqc.IR.ProgramMembers;

namespace Tqc.IR.References;

public record StructTypeReference(IStruct Struct) : Ref, IBuiltinTypeRef
{
    public override string ToString() => Struct.Name;
}
