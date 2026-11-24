using Tqc.IR.Nodes;

namespace Tqc.IR.ProgramMembers.Tq;

public record TqParameter
{
    public bool Lazy { get; init; }
    public IrExpression Type { get; set; }
    public string Name { get; init; }
    public IrExpression? DefaultValue { get; set; }

    public TqParameter(
        IrExpression type,
        string name,
        IrExpression? defaultValue = null,
        bool lazy = false
    )
    {
        Lazy = lazy;
        Type = type;
        Name = name;
        DefaultValue = defaultValue;
    }

    // Each parameter is its own: the `T` of `List` and the `T` of `IEnumerable` have the same name and the same type, and
    // are not the same parameter. With the equality of a record they would be, and the instance table would join
    // `IEnumerator(T)` of one with the one of the other.
    public virtual bool Equals(TqParameter? other) => ReferenceEquals(this, other);
    public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
}
