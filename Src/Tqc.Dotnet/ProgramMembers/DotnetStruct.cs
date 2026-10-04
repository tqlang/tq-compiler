using Mono.Cecil;
using Tqc.IR.ProgramMembers;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetStruct(TypeReference typeRef, string name) : IStruct, IDotnetType
{
    public TypeReference TypeReference => typeRef;
    public string Name => name;
    
    public override string ToString() => $"struct {name}";
}
