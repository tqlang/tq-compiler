using Tqc.IR.Nodes;
using Tqc.IR.References;

namespace Tqc.IR;

/// <summary>
/// The type of each typed node. It lives beside the IR so nodes stay immutable records.
/// Nodes are compared by identity: two equal records at different places are different nodes.
/// </summary>
public sealed class TypeTable
{
    private readonly Dictionary<IrNode, ITypeRef> _types = new(ReferenceEqualityComparer.Instance);

    public void Set(IrNode node, ITypeRef type) => _types[node] = type;

    /// <summary>Null if the node has no known type (yet).</summary>
    public ITypeRef? Get(IrNode node) => _types.GetValueOrDefault(node);
}
