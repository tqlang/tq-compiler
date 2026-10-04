using Mono.Cecil;
using Tqc.IR.ProgramMembers;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetModule : IModule
{
    public AssemblyDefinition[] Assemblies { get; init; }
    public DotnetNamespace RootNamespace { get; init; }
    INamespace IModule.RootNamespace => RootNamespace;
}
