using System.Collections.Immutable;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.IR.ProgramMembers.Primitive;

/// <summary>
/// The part of the `Std` module that the compiler creates. It exists even when no Std library is provided;
/// when one is, the two are merged (see StdMerger).
/// </summary>
public static class CompilerStd
{
    public const string ModuleName = "Std";

    public static readonly StdTypes Types = new();

    static CompilerStd() => Types.DeclareIntrinsics();

    public static readonly IModule Module = new PrimitiveModule(
        new PrimitiveNamespace(ModuleName,
        [
            new PrimitiveNamespace("Types", Types.All),
            new PrimitiveNamespace("Memory", [new PrimitiveNamespace(StdMemoryUnsafe.NamespaceName, StdMemoryUnsafe.Members)]),
            new PrimitiveNamespace(StdMath.NamespaceName, [StdMath.Operator]),
            new PrimitiveNamespace(StdType.NamespaceName, StdType.Members),
        ]));
}

/// <summary>The members of `Std.Types`.</summary>
public sealed class StdTypes
{
    public PrimitiveStruct Bool { get; } = new("Bool");
    public PrimitiveStruct Char { get; } = new("Char");
    public PrimitiveStruct String { get; } = new("String");

    public PrimitiveStruct Void { get; } = new("Void");
    public PrimitiveStruct Opaque { get; } = new("Opaque");
    public PrimitiveStruct NoReturn { get; } = new("NoReturn");
    public PrimitiveStruct Label { get; } = new("Label");

    // Types of comptime parameters, e.g. `func assert(type T, anytype other)`.
    public PrimitiveStruct Type { get; } = new("Type");
    public PrimitiveStruct AnyType { get; } = new("AnyType");

    /// <summary>
    /// `Const(T)`: what `const` is. `const T x` is `let Const(T) x` and `const x` is `let Const(AnyType) x`, with
    /// the type taken from the value. A constant has the values of `T`, and cannot be assigned.
    /// </summary>
    public PrimitiveStruct Const { get; }

    /// <summary>`Ref(T)`: a reference to a `T`. `*T` is `Ref(T)`, and so is a struct that is by reference (a `@class`, a .NET class).</summary>
    public PrimitiveStruct Ref { get; }

    /// <summary>`Failable(T)`: a `T` or an error. `!T` is `Failable(T)`, and `!void` is a function that can fail without a result.</summary>
    public PrimitiveStruct Failable { get; }

    /// <summary>`Nullable(T)`: a `T` or nothing. `?T` is `Nullable(T)`.</summary>
    public PrimitiveStruct Nullable { get; }

    /// <summary>
    /// `UnsafePtr(T)`: an unmanaged pointer to a `T`, as in `T*` of .NET (`void*` is `UnsafePtr(Opaque)`). It is not a `Ref(T)` and never
    /// converts to one: only `null` is a value of it for now; what else it does (dereference, index, convert) will be intrinsic functions.
    /// </summary>
    public PrimitiveStruct UnsafePtr { get; }

    /// <summary>`int` and `uint`: the integer of the target's default size.</summary>
    public PrimitiveStruct NaturalInteger { get; }

    /// <summary>`iptr` and `uptr`: the integer of the size of a pointer.</summary>
    public PrimitiveStruct NativeInteger { get; }

    /// <summary>`i8`..`i256` and `u8`..`u256`.</summary>
    public PrimitiveStruct Integer { get; }

    /// <summary>`f16`..`f128`.</summary>
    public PrimitiveStruct Float { get; }

    public StdTypes()
    {
        Const = new PrimitiveStruct("Const", [Parameter("type", Type)]);
        Ref = new PrimitiveStruct("Ref", [Parameter("type", Type)]);
        Failable = new PrimitiveStruct("Failable", [Parameter("type", Type)]);
        Nullable = new PrimitiveStruct("Nullable", [Parameter("type", Type)]);
        UnsafePtr = new PrimitiveStruct("UnsafePtr", [Parameter("type", Type)]);
        NaturalInteger = new PrimitiveStruct("NaturalInteger", [Parameter("signed", Bool)]);
        NativeInteger = new PrimitiveStruct("NativeInteger", [Parameter("signed", Bool)]);

        var bitSize = new TqParameter(Expression(Natural(true).GetReference()), "bitSize");
        Integer = new PrimitiveStruct("Integer", [Parameter("signed", Bool), bitSize]);
        Float = new PrimitiveStruct("Float", [new TqParameter(Expression(Natural(true).GetReference()), "bitSize")]);
    }

    /// <summary>
    /// What the types can do. Each member is intrinsic: the backend lowers a call to it. It is declared once `CompilerStd.Types`
    /// exists, because a member's type is made from the instance table, which needs it.
    /// </summary>
    internal void DeclareIntrinsics()
    {
        // The bounds of an integer: `u8.max`, `i32.min`. They are constants of the type that they are read from.
        foreach (var integer in new[] { Integer, NaturalInteger, NativeInteger })
        {
            integer.Members =
            [
                new IntrinsicProperty("min", integer, null) { IsStatic = true },
                new IntrinsicProperty("max", integer, null) { IsStatic = true },
            ];
        }

        // What the wrappers can do. Each is intrinsic: the backend lowers a call to it (`x.unwrap()`, `x.value`, `x.ignored()`).
        var maybe = new GenericParameterTypeRef(Nullable.Parameters[0]);
        Nullable.Members =
        [
            new IntrinsicMethodGroup("unwrap", [new IntrinsicMethod("unwrap", Nullable, [], maybe)]),
            new IntrinsicProperty("value", Nullable, maybe),
        ];

        // `!void` has nothing to keep, so what `ignored` gives there is a `?void`, which is nothing.
        var result = new GenericParameterTypeRef(Failable.Parameters[0]);
        Failable.Members =
        [
            new IntrinsicMethodGroup("ignored", [new IntrinsicMethod("ignored", Failable, [], NullableOf(result))]),
        ];
    }

    public IEnumerable<IMember> All =>
    [
        Bool, Char, String, Void, Opaque, NoReturn, Label, Type, AnyType,
        NaturalInteger, NativeInteger, Integer, Float, Const, Ref, Failable, Nullable, UnsafePtr,
    ];

    /// <summary>`definition(argument)` as an expression, for a pseudotype that modifies a type (`Const`, `Ref`, `Failable`).</summary>
    public IrExpression Apply(PrimitiveStruct definition, Tqc.Core.SourceLocation location, IrExpression argument) => new IrCall(
        location,
        new IrReference(location, definition.GetReference()),
        new IrArgumentsList(location, [argument]));

    /// <summary>
    /// The type expression `Const(T)` of a constant declared with `const`; without a type (`const x = 1`) the
    /// argument is `AnyType`, which says that the type comes from the value.
    /// </summary>
    public IrExpression ConstOf(Tqc.Core.SourceLocation location, IrExpression? type) =>
        Apply(Const, location, type ?? new IrReference(location, AnyType.GetReference()));

    /// <summary>The type `Ref(T)`.</summary>
    public ITypeRef RefOf(ITypeRef referred) => OfType(Ref, referred);

    /// <summary>The type `Failable(T)`.</summary>
    public ITypeRef FailableOf(ITypeRef result) => OfType(Failable, result);

    /// <summary>The type `Nullable(T)`.</summary>
    public ITypeRef NullableOf(ITypeRef value) => OfType(Nullable, value);

    /// <summary>The type `UnsafePtr(T)`.</summary>
    public ITypeRef UnsafePtrOf(ITypeRef pointee) => OfType(UnsafePtr, pointee);

    /// <summary>Whether a type is `UnsafePtr(T)`; `pointee` is `T` as written.</summary>
    public bool IsUnsafePtr(ITypeRef? type, out ITypeRef pointee) => TryUnary(type, UnsafePtr, out pointee);

    /// <summary>Whether a type is `Nullable(T)`; `value` is `T` as written.</summary>
    public bool IsNullable(ITypeRef? type, out ITypeRef value) => TryUnary(type, Nullable, out value);

    /// <summary>
    /// The pseudotype a unary operator stands for when its operand is a type: `!T` is `Failable(T)` and `?T` is
    /// `Nullable(T)`. Null for any other operator.
    /// </summary>
    public PrimitiveStruct? PseudotypeOf(UnaryOperator @operator) => @operator switch
    {
        UnaryOperator.Not => Failable,
        UnaryOperator.Nullable => Nullable,
        _ => null,
    };

    /// <summary>Whether a type is `Ref(T)`; `referred` is `T` as written.</summary>
    public bool IsRef(ITypeRef? type, out ITypeRef referred) => TryUnary(type, Ref, out referred);

    /// <summary>Whether a type is `Failable(T)`; `result` is `T` as written.</summary>
    public bool IsFailable(ITypeRef? type, out ITypeRef result) => TryUnary(type, Failable, out result);

    private static ITypeRef OfType(PrimitiveStruct definition, ITypeRef argument)
    {
        var expression = TypeExpressions.Of(argument, default) ?? new IrReference(default, CompilerStd.Types.AnyType.GetReference());
        return new StructTypeReference(GenericInstanceTable.Shared.GetOrAdd(definition, [expression]));
    }

    private static bool TryUnary(ITypeRef? type, PrimitiveStruct definition, out ITypeRef argument)
    {
        argument = null!;
        if (type is not StructTypeReference { Struct: GenericInstance { Arguments: [var only] } instance }
            || !ReferenceEquals(instance.Definition, definition)) return false;

        var raw = TypeExpressions.Raw(only);
        if (raw == null) return false;

        argument = raw;
        return true;
    }

    public GenericInstance Natural(bool signed) => Instance(NaturalInteger, BoolArgument(signed));
    public GenericInstance Native(bool signed) => Instance(NativeInteger, BoolArgument(signed));
    public GenericInstance IntegerOf(bool signed, int bitSize) => Instance(Integer, BoolArgument(signed), IntArgument(bitSize));
    public GenericInstance FloatOf(int bitSize) => Instance(Float, IntArgument(bitSize));

    private static GenericInstance Instance(PrimitiveStruct definition, params IrExpression[] arguments) =>
        GenericInstanceTable.Shared.GetOrAdd(definition, [..arguments]);

    private static IrComptimeBool BoolArgument(bool value) => new(default, value);
    private static IrComptimeInt IntArgument(int value) => new(default, value);

    private static TqParameter Parameter(string name, PrimitiveStruct type) => new(Expression(type.GetReference()), name);
    private static IrReference Expression(Tqc.IR.References.Ref reference) => new(default, reference);
}
