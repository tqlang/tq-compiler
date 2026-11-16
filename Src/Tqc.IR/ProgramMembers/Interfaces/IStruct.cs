using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.IR.ProgramMembers;

public interface IStruct : IType
{
    public string Name { get; }

    /// <summary>Generic parameters. A struct with parameters is a type constructor, not a type.</summary>
    public IReadOnlyList<TqParameter> Parameters => [];
}
