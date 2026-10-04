using Mono.Cecil;
using Tqc.IR.ProgramMembers;
using Tqc.IR.References;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetMethod(string name, ITypeRef? returnType, IReadOnlyList<ITypeRef> parameterTypes, MethodReference reference)
    : IFunction, IDotnetMember
{
    public string Name => name;
 
    /// <summary>Null significa void.</summary>
    public ITypeRef? ReturnType => returnType;
    public IReadOnlyList<ITypeRef> ParameterTypes => parameterTypes;
 
    public MethodReference Reference => reference;
    public bool IsStatic => !reference.HasThis;
    public bool IsConstructor => name == ".ctor";
 
    public override string ToString() => $"method {name}";
}
