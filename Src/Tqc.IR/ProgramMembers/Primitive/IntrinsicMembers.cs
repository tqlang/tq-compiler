using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.IR.ProgramMembers.Primitive;

/// <summary>
/// A function that the compiler declares on one of the types it creates (`Nullable(T).unwrap()`). It has no body: it is
/// intrinsic, so the backend knows what it stands for and lowers a call to it. The type checker treats it as any other method.
/// </summary>
public sealed class IntrinsicMethod(
    string name, PrimitiveStruct owner, IReadOnlyList<ITypeRef> parameterTypes, ITypeRef? returnType) : IMethod
{
    public string Name => name;
    public PrimitiveStruct Owner => owner;

    /// <summary>Of the parameters that a call gives; the value that it is called on is not one of them.</summary>
    public IReadOnlyList<ITypeRef> ParameterTypes => parameterTypes;

    /// <summary>Null: it returns nothing.</summary>
    public ITypeRef? ReturnType => returnType;

    public bool IsStatic => false;
    public bool IsConstructor => false;
    public bool IsAbstract => false;
    public bool CanBeOverridden => false;

    public override string ToString() => $"intrinsic {owner.Name}.{name}";
}

/// <summary>The intrinsic methods of a type that share a name: its overloads.</summary>
public sealed class IntrinsicMethodGroup(string name, IReadOnlyList<IntrinsicMethod> methods) : IFunctionGroup
{
    public string Name => name;
    public IReadOnlyList<IntrinsicMethod> Methods => methods;

    public override string ToString() => $"intrinsic group {name}";
}

/// <summary>
/// A value that the compiler declares on one of its types (`Nullable(T).value`, `Integer(...).max`); it can only be read.
/// A static one belongs to the type (`u8.max`), and its type is the type itself (`Type` is null).
/// </summary>
public sealed class IntrinsicProperty(string name, PrimitiveStruct owner, ITypeRef? type) : IField
{
    public string Name => name;
    public PrimitiveStruct Owner => owner;

    /// <summary>Null for a static one: the type that it is read from.</summary>
    public ITypeRef? Type => type;

    public bool IsStatic { get; init; }

    public override string ToString() => $"intrinsic {owner.Name}.{name}";
}
