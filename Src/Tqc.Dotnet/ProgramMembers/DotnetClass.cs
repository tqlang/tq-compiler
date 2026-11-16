using System.Collections.Immutable;
using Mono.Cecil;
using Tqc.IR.ProgramMembers;
using Tqc.IR.References;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetClass(TypeReference typeRef, string name) : IStruct, IDotnetType, IObjectType
{
    
    public TypeReference TypeReference => typeRef;
    public string Name => name;

    // What the metadata says; a type whose assembly cannot be loaded is taken as having no restrictions.
    public bool IsAbstract => DotnetMetadata.Definition(typeRef) is { IsAbstract: true };
    public bool IsInterface => false;
    public bool IsFinal => DotnetMetadata.Definition(typeRef) is { IsSealed: true };
    public bool IsReference => true;
    
    public ImmutableDictionary<string, IMember> Members { get; set; }
    public ITypeRef? BaseType { get; set; }

    public IEnumerable<IMember> OwnMembers => Members?.Values ?? [];

    public IMemberContainer? Base =>
        BaseType is StructTypeReference { Struct: IMemberContainer container } ? container : null;

    /// <summary>A member of the class or, failing that, of its base classes.</summary>
    public IMember? TryGetMember(string memberName)
    {
        HashSet<IMemberContainer> visited = [];

        for (IMemberContainer? current = this; current != null && visited.Add(current); current = current.Base)
        {
            var member = current is DotnetClass dotnet
                ? dotnet.Members?.GetValueOrDefault(memberName)
                : current.TryGetMember(memberName);
            if (member != null) return member;
        }

        return null;
    }
    
    public override string ToString() => $"class {name}";
}
