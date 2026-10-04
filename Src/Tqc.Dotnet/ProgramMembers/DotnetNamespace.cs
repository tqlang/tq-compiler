using System.Collections.Immutable;
using Tqc.IR.ProgramMembers;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetNamespace(string name) : INamespace, IDotnetMember
{
    public string Name => name;
    public ImmutableDictionary<string, INamespace> Namespaces { get; init; }
    public ImmutableDictionary<string, IDotnetType> Types { get; init; }

    public INamespace? TryGetNamespace(string namespaceName) => Namespaces.GetValueOrDefault(namespaceName);
    public IMember? TryGetMember(string memberName) => (IMember?)Types.GetValueOrDefault(memberName) ?? Namespaces.GetValueOrDefault(memberName);
    public IMember[] GetAllMembers() => [
        ..Types.Select(IMember (e) => e.Value),
        ..Namespaces.Select(IMember (e) => e.Value),
    ];
    
    public override string ToString() => $"dotnet namespace {Name}";
}
