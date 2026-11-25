using Mono.Cecil;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Dotnet.ProgramMembers;

public class DotnetMethod(
    string name, ITypeRef? returnType, IReadOnlyList<ITypeRef> parameterTypes, MethodReference reference,
    IReadOnlyList<TqParameter>? typeParameters = null, bool isExtension = false)
    : IMethod, IDotnetMember
{
    public string Name => name;
 
    /// <summary>Null significa void.</summary>
    public ITypeRef? ReturnType => returnType;
    public IReadOnlyList<ITypeRef> ParameterTypes => parameterTypes;
 
    /// <summary>
    /// The generic parameters of the method (`T` of `AllocateArray&lt;T&gt;`). In Tq they are the first parameters of the call,
    /// of the type `type`: `GC.AllocateArray(i8, 10)`. They are not in <see cref="ParameterTypes"/>.
    /// </summary>
    public IReadOnlyList<TqParameter> TypeParameters => typeParameters ?? [];

    public MethodReference Reference => reference;
    public bool IsStatic => !reference.HasThis;
    public bool IsConstructor => name == ".ctor";

    /// <summary>
    /// A static method whose first parameter is the one it extends (`this` in C#): `WindowExtensions.Run(window)` is also
    /// `window.Run()`. What the metadata says; false if the assembly of the method cannot be loaded.
    /// </summary>
    public bool IsExtension => IsStatic && parameterTypes.Count > 0
        && (isExtension || Definition is { } definition
            && definition.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.ExtensionAttribute"));

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
