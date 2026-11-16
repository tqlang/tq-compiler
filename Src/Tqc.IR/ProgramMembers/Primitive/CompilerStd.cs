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

    public static readonly IModule Module = new PrimitiveModule(
        new PrimitiveNamespace(ModuleName,
        [
            new PrimitiveNamespace("Types", Types.All),
            new PrimitiveNamespace("Memory", []),
            new PrimitiveNamespace(StdMath.NamespaceName, [StdMath.Operator]),
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
        NaturalInteger = new PrimitiveStruct("NaturalInteger", [Parameter("signed", Bool)]);
        NativeInteger = new PrimitiveStruct("NativeInteger", [Parameter("signed", Bool)]);

        var bitSize = new TqParameter(Expression(Natural(true).GetReference()), "bitSize");
        Integer = new PrimitiveStruct("Integer", [Parameter("signed", Bool), bitSize]);
        Float = new PrimitiveStruct("Float", [new TqParameter(Expression(Natural(true).GetReference()), "bitSize")]);
    }

    public IEnumerable<IMember> All =>
    [
        Bool, Char, String, Void, Opaque, NoReturn, Label, Type, AnyType,
        NaturalInteger, NativeInteger, Integer, Float,
    ];

    public GenericInstance Natural(bool signed) => Instance(NaturalInteger, BoolArgument(signed));
    public GenericInstance Native(bool signed) => Instance(NativeInteger, BoolArgument(signed));
    public GenericInstance IntegerOf(bool signed, int bitSize) => Instance(Integer, BoolArgument(signed), IntArgument(bitSize));
    public GenericInstance FloatOf(int bitSize) => Instance(Float, IntArgument(bitSize));

    private static GenericInstance Instance(PrimitiveStruct definition, params IrExpression[] arguments) =>
        GenericInstanceTable.Shared.GetOrAdd(definition, [..arguments]);

    private static IrComptimeBool BoolArgument(bool value) => new(default, value);
    private static IrComptimeInt IntArgument(int value) => new(default, value);

    private static TqParameter Parameter(string name, PrimitiveStruct type) => new(Expression(type.GetReference()), name);
    private static IrReference Expression(Ref reference) => new(default, reference);
}
