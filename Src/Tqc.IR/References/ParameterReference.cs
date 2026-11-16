using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.IR.References;

public sealed record ParameterReference(TqParameter Parameter) : Ref
{
    public override string ToString() => Parameter.Name;
}
