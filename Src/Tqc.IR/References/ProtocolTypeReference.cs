using Tqc.IR.ProgramMembers;

namespace Tqc.IR.References;

public record ProtocolTypeReference(IProtocol Protocol) : Ref, IBuiltinTypeRef
{
    public override string ToString() => Protocol.Name;
}
