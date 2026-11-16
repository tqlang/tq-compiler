using Mono.Cecil;
using Tqc.IR.ProgramMembers;
using Tqc.IR.References;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetMethod(string name, ITypeRef? returnType, IReadOnlyList<ITypeRef> parameterTypes, MethodReference reference)
    : IMethod, IDotnetMember
{
    public string Name => name;
 
    /// <summary>Null significa void.</summary>
    public ITypeRef? ReturnType => returnType;
    public IReadOnlyList<ITypeRef> ParameterTypes => parameterTypes;
 
    public MethodReference Reference => reference;
    public bool IsStatic => !reference.HasThis;
    public bool IsConstructor => name == ".ctor";

    // What the metadata says; false if the assembly of the method cannot be loaded.
    public bool IsAbstract => Definition is { IsAbstract: true };
    public bool IsVirtual => Definition is { IsVirtual: true, IsFinal: false };

    /// <summary>Virtual, abstract and interface methods can be replaced; the last two are always virtual in the metadata.</summary>
    public bool CanBeOverridden => IsVirtual || IsAbstract;

    private MethodDefinition? Definition
    {
        get
        {
            try { return reference.Resolve(); }
            catch (AssemblyResolutionException) { return null; }
        }
    }
 
    public override string ToString() => $"method {name}";
}
