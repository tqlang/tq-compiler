using Tqc.IR.ProgramMembers;

namespace Tqc.IR.References;

public record FunctionTypeReference(IFunction Function) : Ref, IBuiltinTypeRef
{
    public override string ToString() => Function.Name;
}
