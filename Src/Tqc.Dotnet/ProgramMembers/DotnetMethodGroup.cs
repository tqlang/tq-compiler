using System.Collections.Immutable;
using Tqc.IR.ProgramMembers;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetMethodGroup(string name) : IFunctionGroup, IDotnetMember
{
    public string Name => name;
    public ImmutableArray<DotnetMethod> Methods = [];
    
    public override string ToString() => $"method_group {name}";
}
