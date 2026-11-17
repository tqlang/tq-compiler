using Mono.Cecil;
using Tqc.IR.ProgramMembers;
using Tqc.IR.References;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetProperty(
    string name,
    ITypeRef type,
    IReadOnlyList<ITypeRef> indexParameterTypes,
    MethodReference? getter,
    MethodReference? setter,
    DotnetMethod? getMethod,
    DotnetMethod? setMethod
) : IField, IDotnetMember
{
    public string Name => name;
    public ITypeRef Type => type;
    public MethodReference? Getter => getter;
    public MethodReference? Setter => setter;

    /// <summary>The parameters of an indexed property (`this[int i]`); none for an ordinary one.</summary>
    public IReadOnlyList<ITypeRef> IndexParameterTypes => indexParameterTypes;

    /// <summary>An indexed property is the indexer of the type: `a[i]` calls its accessors.</summary>
    public bool IsIndexer => indexParameterTypes.Count > 0;

    /// <summary>The accessors as methods, so that they are chosen like any other method.</summary>
    public DotnetMethod? GetMethod => getMethod;
    public DotnetMethod? SetMethod => setMethod;
 
    public override string ToString() => $"property {name}";
}
