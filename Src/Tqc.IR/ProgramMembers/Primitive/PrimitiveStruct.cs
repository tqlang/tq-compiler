using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.IR.ProgramMembers.Primitive;

/// <summary>A struct created by the compiler. It has no body; with parameters it is generic.</summary>
public class PrimitiveStruct(string name, IReadOnlyList<TqParameter>? parameters = null) : IStruct
{
    public string Name => name;
    public IReadOnlyList<TqParameter> Parameters => parameters ?? [];

    public override string ToString() => $"struct {Name}";
}
