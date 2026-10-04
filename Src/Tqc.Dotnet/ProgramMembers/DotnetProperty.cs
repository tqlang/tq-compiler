using Mono.Cecil;
using Tqc.IR.ProgramMembers;
using Tqc.IR.References;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetProperty(
    string name,
    ITypeRef type,
    MethodReference? getter,
    MethodReference? setter
) : IField, IDotnetMember
{
    public string Name => name;
    public ITypeRef Type => type;
    public MethodReference? Getter => getter;
    public MethodReference? Setter => setter;
 
    public override string ToString() => $"property {name}";
}
