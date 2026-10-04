using Mono.Cecil;
using Tqc.IR.ProgramMembers;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetEnum(TypeReference typeRef, string name) : IStruct, IDotnetType
{
    public TypeReference TypeReference => typeRef;
    public string Name => name;
    
    public override string ToString() => $"enum {name}";
}
