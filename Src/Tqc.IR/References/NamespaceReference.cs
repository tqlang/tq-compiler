using Tqc.IR.ProgramMembers;

namespace Tqc.IR.References;

public record NamespaceReference(INamespace Namespace) : Ref, IBuiltinTypeRef
{
    public override string ToString() => Namespace.Name;
}
