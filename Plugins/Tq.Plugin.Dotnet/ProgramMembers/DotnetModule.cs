using Mono.Cecil;
using Tq.Core.Language.Members;

namespace Tq.Plugin.Dotnet.ProgramMembers;

public class DotnetModule(string name) : BaseModule(name)
{
    public List<AssemblyDefinition> Assemblies = [];
    public Dictionary<string, List<(TypeReference referece, TypeDefinition definition)>> TypeDefinitions = [];
}
