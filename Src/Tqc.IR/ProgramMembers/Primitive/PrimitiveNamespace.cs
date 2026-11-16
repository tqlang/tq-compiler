namespace Tqc.IR.ProgramMembers.Primitive;

public class PrimitiveNamespace(string name, IEnumerable<IMember> members) : INamespace
{
    private readonly Dictionary<string, IMember> _members = members.ToDictionary(member => member.Name);

    public string Name => name;

    /// <summary>For a namespace made by merging: the one of the project that it was merged from.</summary>
    public INamespace? Original { get; init; }

    public INamespace? TryGetNamespace(string namespaceName) => _members.GetValueOrDefault(namespaceName) as INamespace;
    public IMember? TryGetMember(string memberName) => _members.GetValueOrDefault(memberName);
    public IMember[] GetAllMembers() => [.._members.Values];

    public override string ToString() => $"namespace {Name}";
}
