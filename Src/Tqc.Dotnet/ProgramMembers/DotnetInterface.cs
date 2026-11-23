using System.Collections.Immutable;
using Mono.Cecil;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.References;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetInterface(TypeReference typeRef, string name) : IProtocol, IDotnetType, IObjectType
{
    public TypeReference TypeReference => typeRef;
    public string Name => name;

    // What the metadata says; a type whose assembly cannot be loaded is taken as having no restrictions.
    public bool IsAbstract => DotnetMetadata.Definition(typeRef) is { IsAbstract: true };
    public bool IsInterface => true;
    public bool IsFinal => DotnetMetadata.Definition(typeRef) is { IsSealed: true };
    public virtual bool IsReference => false;

    /// <summary>Set once every type exists, so that members can point to any type.</summary>
    public ImmutableDictionary<string, IMember>? Members { get; set; }

    /// <summary>`System.Object`: what every value of an interface has (not a base: an interface extends none).</summary>
    public ITypeRef? ObjectType { get; set; }

    /// <summary>The interfaces it extends.</summary>
    public IReadOnlyList<ITypeRef> ParentInterfaces { get; set; } = [];

    public IMember? TryGetMember(string memberName)
    {
        if (Members?.GetValueOrDefault(memberName) is { } own) return own;

        foreach (var parent in ParentInterfaces)
        {
            // `IEnumerable(T)` as a parent: its members are the ones of the generic interface.
            var written = CompilerStd.Types.IsRef(parent, out var referred) ? referred : parent;
            IMemberContainer? container = written switch
            {
                ProtocolTypeReference { Protocol: IMemberContainer protocol } => protocol,
                StructTypeReference { Struct: GenericInstance { Definition: IMemberContainer definition } } => definition,
                _ => null,
            };

            if (container?.TryGetMember(memberName) is { } inherited) return inherited;
        }

        return DotnetInheritance.ObjectOf(ObjectType)?.TryGetMember(memberName);
    }
    public IEnumerable<IMember> OwnMembers => Members?.Values ?? [];

    public override string ToString() => $"interface {name}";
}
