using System.Collections.Immutable;
using Mono.Cecil;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetStruct(TypeReference typeRef, string name) : IStruct, IDotnetType, IObjectType
{
    public TypeReference TypeReference => typeRef;
    public string Name => name;

    /// <summary>Generic parameters (`T` of `List&lt;T&gt;`): the type is a constructor of types, used as `List(i8)`.</summary>
    public IReadOnlyList<TqParameter> Parameters { get; init; } = [];

    // What the metadata says; a type whose assembly cannot be loaded is taken as having no restrictions.
    public bool IsAbstract => DotnetMetadata.Definition(typeRef) is { IsAbstract: true };
    public bool IsInterface => false;
    public bool IsFinal => DotnetMetadata.Definition(typeRef) is { IsSealed: true };
    public bool IsReference => false;

    /// <summary>Set once every type exists, so that members can point to any type.</summary>
    public ImmutableDictionary<string, IMember>? Members { get; set; }

    /// <summary>
    /// `System.Object`, only to find what every value has (`ToString`, `Equals`...). It is not `Base`: a struct of .NET
    /// is by value, whatever `ValueType` and `Object` are.
    /// </summary>
    public ITypeRef? ObjectType { get; set; }

    public IMember? TryGetMember(string memberName) => DotnetInheritance.Find(this, Members, memberName, DotnetInheritance.ObjectOf(ObjectType));
    public IEnumerable<IMember> OwnMembers => Members?.Values ?? [];
    
    public override string ToString() => $"struct {name}";
}
