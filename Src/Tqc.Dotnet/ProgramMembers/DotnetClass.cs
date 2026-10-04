using System.Collections.Immutable;
using Mono.Cecil;
using Tqc.IR.ProgramMembers;
using Tqc.IR.References;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetClass(TypeReference typeRef, string name) : IStruct, IDotnetType
{
    
    public TypeReference TypeReference => typeRef;
    public string Name => name;
    
    public ImmutableDictionary<string, IMember> Members { get; set; }
    public ITypeRef? BaseType { get; set; }

    
    public override string ToString() => $"class {name}";
}
