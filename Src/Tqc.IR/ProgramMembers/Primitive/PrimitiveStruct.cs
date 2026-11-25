using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.IR.ProgramMembers.Primitive;

/// <summary>
/// A struct created by the compiler. It has no body; with parameters it is generic. What it can do is declared in
/// <see cref="Members"/> as intrinsic members (see <see cref="IntrinsicMethod"/>).
/// </summary>
public class PrimitiveStruct(string name, IReadOnlyList<TqParameter>? parameters = null) : IStruct, IMemberContainer
{
    public string Name => name;
    public IReadOnlyList<TqParameter> Parameters => parameters ?? [];

    /// <summary>The intrinsic members of the type: methods (as groups) and properties.</summary>
    public IReadOnlyList<IMember> Members { get; set; } = [];

    public IMember? TryGetMember(string memberName) => Members.FirstOrDefault(member => member switch
    {
        IntrinsicMethodGroup group => group.Name == memberName,
        IntrinsicProperty property => property.Name == memberName,
        _ => false,
    });

    public IEnumerable<IMember> OwnMembers => Members;

    public override string ToString() => $"struct {Name}";
}
