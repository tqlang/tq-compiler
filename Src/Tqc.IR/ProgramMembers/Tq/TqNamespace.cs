using System.Collections.Immutable;

namespace Tqc.IR.ProgramMembers.Tq;

public class TqNamespace(string name) : INamespace
{
    public string Name => name;

    public ImmutableDictionary<string, TqNamespace> Namespaces { get; init; }
    public ImmutableDictionary<string, IMember> Members { get; init; }
    public ImmutableArray<TqFileScope> FileScopes { get; init; }
    
    public INamespace? TryGetNamespace(string namespaceName) => Namespaces.GetValueOrDefault(namespaceName);
    public IMember? TryGetMember(string memberName) => Members.GetValueOrDefault(memberName) ?? Namespaces.GetValueOrDefault(memberName);
    public IMember[] GetAllMembers() => [
        ..Members.Select(IMember (e) => e.Value),
        ..Namespaces.Select(IMember (e) => e.Value)
    ];

    public override string ToString() => $"namespace {Name}";
}
