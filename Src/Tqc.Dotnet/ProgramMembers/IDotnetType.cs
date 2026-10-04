using Mono.Cecil;

namespace Tqc.Dotnet.ProgramMembers;

public interface IDotnetType : IDotnetMember
{
    string Name { get; }
    TypeReference TypeReference { get; }
}
