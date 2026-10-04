using Mono.Cecil;
using Tqc.IR.ProgramMembers;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetInterface(TypeReference typeRef, string name) : IProtocol, IDotnetType
{
    public TypeReference TypeReference => typeRef;
    public string Name => name;

    public override string ToString() => $"interface {name}";
}
