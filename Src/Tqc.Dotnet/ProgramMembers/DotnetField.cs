using Mono.Cecil;
using Tqc.IR.ProgramMembers;
using Tqc.IR.References;
using FieldReference = Mono.Cecil.FieldReference;

namespace Tqc.Dotnet.ProgramMembers;

 
public class DotnetField(string name, ITypeRef type, FieldReference reference, bool isStatic, bool isConstant, object? constant)
    : IField, IDotnetMember
{
    public string Name => name;
    public ITypeRef Type => type;
    public FieldReference Reference => reference;
    public bool IsStatic => isStatic;
    public bool IsConstant => isConstant;
    public object? Constant => constant;
 
    public override string ToString() => $"field {name}";
}
