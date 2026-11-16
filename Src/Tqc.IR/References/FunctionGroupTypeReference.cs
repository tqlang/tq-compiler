using Tqc.IR.ProgramMembers;

namespace Tqc.IR.References;

public record FunctionGroupTypeReference(IFunctionGroup FunctionGroup) : Ref, IBuiltinTypeRef
{
    public override string ToString() => FunctionGroup.Name;
}
