using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.IR.References;

/// <summary>
/// `field` inside an accessor of a property: the storage of the property. The IR never declares it; the backend
/// creates it when <see cref="TqProperty.HasBackingField"/> is true.
/// </summary>
public sealed record BackingFieldReference(TqProperty Property) : Ref
{
    public override string ToString() => $"field ({Property.Name})";
}
