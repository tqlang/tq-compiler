using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.IR.ProgramMembers.Primitive;

/// <summary>What an intrinsic of `Std.Memory.Unsafe` does; the backend lowers a call to it.</summary>
public enum UnsafeOperation
{
    /// <summary>`ptrFromInt(type T, iptr|uptr address) UnsafePtr(T)`</summary>
    PtrFromInt,

    /// <summary>`intFromPtr(UnsafePtr(Opaque) | Ref(anything)) uptr`</summary>
    IntFromPtr,

    /// <summary>`sliceFromPtr(type T, UnsafePtr(T) | Ref(T) first, uptr length) []T`</summary>
    SliceFromPtr,

    /// <summary>`ptrCast(type T, UnsafePtr(anything) pointer) UnsafePtr(T)`</summary>
    PtrCast,

    /// <summary>`ptrAdd(type T, UnsafePtr(T) pointer, iptr offset) UnsafePtr(T)`: the offset is in elements.</summary>
    PtrAdd,

    /// <summary>`ptrDiff(type T, UnsafePtr(T) left, UnsafePtr(T) right) iptr`: the difference in elements.</summary>
    PtrDiff,

    /// <summary>`refFromPtr(type T, UnsafePtr(T) pointer) Ref(T)`</summary>
    RefFromPtr,

    /// <summary>`ptrFromRef(type T, Ref(T) reference) UnsafePtr(T)`</summary>
    PtrFromRef,

    /// <summary>`bitCast(type To, type From, From value) To`: the same bits as another type of the same size.</summary>
    BitCast,

    /// <summary>`alignOf(type T) uint`: the alignment of `T` in bytes.</summary>
    AlignOf,
}

/// <summary>
/// `Std.Memory.Unsafe`: the operations on memory that the language does not do by itself, on `UnsafePtr(T)` (the pointers
/// that .NET calls `T*`) and on `Ref(T)`. They are intrinsic: they have no body, the backend lowers a call to them to
/// what it can (to a few instructions in .NET) and, if it cannot, to a call that throws. Nothing in them is checked at run time.
/// </summary>
public static class StdMemoryUnsafe
{
    public const string NamespaceName = "Unsafe";

    private static readonly Dictionary<TqFunction, UnsafeOperation> Operations = new(ReferenceEqualityComparer.Instance);
    private static readonly List<TqFunction> Functions = [];

    static StdMemoryUnsafe()
    {
        var types = CompilerStd.Types;
        var native = new StructTypeReference(types.Native(true));
        var unsignedNative = new StructTypeReference(types.Native(false));
        var natural = new StructTypeReference(types.Natural(false));
        var opaquePointer = types.UnsafePtrOf(new StructTypeReference(types.Opaque));
        var anything = new StructTypeReference(types.AnyType);

        // ptrFromInt takes either kind of native integer.
        foreach (var address in new[] { native, unsignedNative })
            Declare("ptrFromInt", UnsafeOperation.PtrFromInt, [T], [("address", _ => address)], t => types.UnsafePtrOf(t[0]));

        Declare("intFromPtr", UnsafeOperation.IntFromPtr, [], [("pointer", _ => opaquePointer)], _ => unsignedNative);
        Declare("intFromPtr", UnsafeOperation.IntFromPtr, [], [("reference", _ => types.RefOf(anything))], _ => unsignedNative);

        Declare("sliceFromPtr", UnsafeOperation.SliceFromPtr, [T], [("pointer", t => types.UnsafePtrOf(t[0])), ("length", _ => unsignedNative)],
            t => new SliceTypeRef(t[0]));
        Declare("sliceFromPtr", UnsafeOperation.SliceFromPtr, [T], [("first", t => types.RefOf(t[0])), ("length", _ => unsignedNative)],
            t => new SliceTypeRef(t[0]));

        Declare("ptrCast", UnsafeOperation.PtrCast, [T], [("pointer", _ => types.UnsafePtrOf(anything))], t => types.UnsafePtrOf(t[0]));
        Declare("ptrAdd", UnsafeOperation.PtrAdd, [T], [("pointer", t => types.UnsafePtrOf(t[0])), ("offset", _ => native)], t => types.UnsafePtrOf(t[0]));
        Declare("ptrDiff", UnsafeOperation.PtrDiff, [T],
            [("left", t => types.UnsafePtrOf(t[0])), ("right", t => types.UnsafePtrOf(t[0]))], _ => native);
        Declare("refFromPtr", UnsafeOperation.RefFromPtr, [T], [("pointer", t => types.UnsafePtrOf(t[0]))], t => types.RefOf(t[0]));
        Declare("ptrFromRef", UnsafeOperation.PtrFromRef, [T], [("reference", t => types.RefOf(t[0]))], t => types.UnsafePtrOf(t[0]));
        Declare("bitCast", UnsafeOperation.BitCast, ["To", "From"], [("value", t => t[1])], t => t[0]);
        Declare("alignOf", UnsafeOperation.AlignOf, [T], [], _ => natural);
    }

    private const string T = "T";

    /// <summary>The functions, each in its group of overloads, as the members of the namespace.</summary>
    public static IEnumerable<IMember> Members =>
        Functions.GroupBy(function => function.Name).Select(group => (IMember)new TqFunctionGroup(group.Key) { Functions = [..group] });

    public static bool TryGet(TqFunction function, out UnsafeOperation operation) => Operations.TryGetValue(function, out operation);

    /// <param name="typeNames">The parameters of the type `type`, first in the call.</param>
    /// <param name="parameters">The others, with their types in terms of the type parameters.</param>
    private static void Declare(
        string name, UnsafeOperation operation, string[] typeNames,
        (string Name, Func<ITypeRef[], ITypeRef> Type)[] parameters, Func<ITypeRef[], ITypeRef> returnType)
    {
        var typeParameters = typeNames
            .Select(typeName => new TqParameter(new IrReference(default, new TypeMetaReference(CompilerStd.Types.Type)), typeName))
            .ToArray();
        var typeArguments = typeParameters.Select(parameter => (ITypeRef)new GenericParameterTypeRef(parameter)).ToArray();

        var function = new TqFunction(name, default)
        {
            Access = new MemberAccess(Visibility.Public, [], [], $"{CompilerStd.ModuleName}.Memory.{NamespaceName}"),
            Parameters = [..typeParameters, ..parameters.Select(parameter => new TqParameter(Expression(parameter.Type(typeArguments)), parameter.Name))],
            ReturnType = Expression(returnType(typeArguments)),
        };

        Operations[function] = operation;
        Functions.Add(function);
    }

    /// <summary>A type as the expression that a parameter or a return type is written with.</summary>
    private static IrExpression Expression(ITypeRef type) => type switch
    {
        StructTypeReference { Struct: GenericInstance instance } => new IrReference(default, instance.GetReference()),
        _ => TypeExpressions.Of(type, default) ?? throw new InvalidOperationException($"The type '{type}' cannot be written as an expression"),
    };
}
