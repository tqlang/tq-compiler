using Tqc.IR.Nodes;

namespace Tqc.IR.References;

/// <summary>Points to the node that declared the variable.</summary>
public sealed record LocalReference(IrLocal Local) : Ref
{
    public override string ToString() => Local.name;
}
