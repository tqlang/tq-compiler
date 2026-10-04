using System.Collections.Immutable;
using Mono.Cecil;
using Tqc.IR.ProgramMembers;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetStaticClass(TypeReference typeRef, string name) : INamespace, IDotnetType
{
    public string Name => name;
    public TypeReference TypeReference => typeRef;

    public ImmutableDictionary<string, DotnetStaticClass> NestedStaticClasses { get; set; }
    public ImmutableDictionary<string, IMember> Members { get; set; }

    public INamespace? TryGetNamespace(string namespaceName) => NestedStaticClasses.GetValueOrDefault(namespaceName);
    public IMember? TryGetMember(string memberName) => Members.GetValueOrDefault(memberName) ?? NestedStaticClasses.GetValueOrDefault(memberName);
    public IMember[] GetAllMembers() => [
            ..Members.Select(IMember (e) => e.Value),
            ..NestedStaticClasses.Select(IMember (e) => e.Value),
        ];
    
    public override string ToString() => $"dotnet static class {Name}";
}
