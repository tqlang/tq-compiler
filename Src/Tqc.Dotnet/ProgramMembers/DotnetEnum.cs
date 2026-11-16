using System.Collections.Immutable;
using Mono.Cecil;
using Tqc.IR.ProgramMembers;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetEnum(TypeReference typeRef, string name) : IStruct, IDotnetType, IObjectType
{
    public TypeReference TypeReference => typeRef;
    public string Name => name;

    // What the metadata says; a type whose assembly cannot be loaded is taken as having no restrictions.
    public bool IsAbstract => DotnetMetadata.Definition(typeRef) is { IsAbstract: true };
    public bool IsInterface => false;
    public bool IsFinal => DotnetMetadata.Definition(typeRef) is { IsSealed: true };
    public bool IsReference => false;

    /// <summary>Set once every type exists, so that members can point to any type.</summary>
    public ImmutableDictionary<string, IMember>? Members { get; set; }

    public IMember? TryGetMember(string memberName) => Members?.GetValueOrDefault(memberName);
    public IEnumerable<IMember> OwnMembers => Members?.Values ?? [];
    
    public override string ToString() => $"enum {name}";
}
