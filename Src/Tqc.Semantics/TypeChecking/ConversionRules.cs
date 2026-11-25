using System.Numerics;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal enum MatchKind
{
    /// <summary>The argument cannot be passed to the parameter.</summary>
    None,

    /// <summary>The argument has exactly the type of the parameter.</summary>
    Exact,

    /// <summary>The argument needs an implicit conversion to the parameter's type.</summary>
    Convert,

    /// <summary>The parameter takes any type (`anytype`, `[]anytype`); the argument is passed as it is.</summary>
    Generic,

    /// <summary>The type of the argument or of the parameter is not known, so nothing can be decided.</summary>
    Unknown,
}

/// <summary>
/// How an argument fits a parameter. A lower cost is a better fit.
/// `Via` is the function of an `@implicitConvert` that does the conversion, when that is what it takes.
/// `Wraps` says the argument is a `T` that goes where a `?T` or a `!T` is asked (after converting it to `T`, if that takes it).
/// </summary>
internal readonly record struct ArgumentMatch(MatchKind Kind, int Cost, IFunction? Via = null, bool Wraps = false)
{
    public static readonly ArgumentMatch None = new(MatchKind.None, 0);
    public static readonly ArgumentMatch Exact = new(MatchKind.Exact, 0);
    public static readonly ArgumentMatch Unknown = new(MatchKind.Unknown, 0);
}

/// <summary>
/// The implicit conversions. Only lossless widening is implicit; everything else needs `as`.
/// </summary>
internal static class ConversionRules
{
    // Costs of a literal: `int` is the default, then `uint`; every other integer type costs the same,
    // so two overloads that differ only there are ambiguous and the literal needs an explicit type.
    private const int LiteralToInt = 1;
    private const int LiteralToUint = 2;
    private const int LiteralToSizedInteger = 3;

    // A decimal literal is an `f64` unless another float is asked for; the other floats tie.
    private const int DecimalToF64 = 1;
    private const int DecimalToOtherFloat = 3;

    // A widening conversion costs more than any literal rule, and grows with the size added.
    private const int Widening = 10;
    private const int Upcast = 10;
    private const int SliceView = 20;
    private const int UserConversion = 100;
    private const int GenericParameter = 1000;
    private const int NullToNullable = 1;
    private const int Wrapping = 1;

    public static ArgumentMatch Classify(
        ITypeRef? argument, IrExpression argumentNode, ITypeRef? parameter,
        ImplicitConversions conversions, AccessContext accessor)
    {
        if (argument == null || parameter == null) return ArgumentMatch.Unknown;

        var types = CompilerStd.Types;

        if (IsStruct(parameter, types.AnyType)) return new ArgumentMatch(MatchKind.Generic, GenericParameter);
        if (IsStruct(parameter, types.Type)) return IsStruct(argument, types.Type) ? ArgumentMatch.Exact : ArgumentMatch.None;

        if (argument.Equals(parameter)) return ArgumentMatch.Exact;

        // `null` is the empty `?T`.
        // It is also the only value that goes where an `UnsafePtr(T)` is asked: the null pointer.
        if (argument is NullTypeRef)
            return types.IsNullable(parameter, out _) || types.IsUnsafePtr(parameter, out _) ? Convert(NullToNullable) : ArgumentMatch.None;

        // A `T` is a `?T` and a `!T`.
        if (types.IsNullable(parameter, out var wrapped) || types.IsFailable(parameter, out wrapped))
        {
            var inner = Classify(argument, argumentNode, wrapped, conversions, accessor);
            if (inner.Kind is MatchKind.Exact or MatchKind.Convert)
                return new ArgumentMatch(MatchKind.Convert, inner.Cost + Wrapping, inner.Via, true);
        }

        if (argument is ComptimeIntegerTypeRef) return ClassifyLiteral(argumentNode, parameter);
        if (argument is ComptimeDecimalTypeRef)
            return TryFloat(parameter, out var floatBits) ? Convert(floatBits == 64 ? DecimalToF64 : DecimalToOtherFloat) : ArgumentMatch.None;

        if (argument is SliceTypeRef && parameter is SliceTypeRef { Element: var element } && IsStruct(element, types.AnyType))
            return new ArgumentMatch(MatchKind.Generic, GenericParameter);

        var widening = ClassifyWidening(argument, parameter);
        if (widening.Kind != MatchKind.None) return widening;

        var upcast = ClassifyUpcast(argument, parameter);
        if (upcast.Kind != MatchKind.None) return upcast;

        var protocol = ClassifyProtocol(argument, parameter);
        if (protocol.Kind != MatchKind.None) return protocol;

        var dotnetProtocol = ClassifyDotnetInterface(argument, parameter);
        if (dotnetProtocol.Kind != MatchKind.None) return dotnetProtocol;

        var via = conversions.Find(argument, parameter, accessor);
        return via != null ? new ArgumentMatch(MatchKind.Convert, UserConversion, via) : ArgumentMatch.None;
    }

    /// <summary>
    /// How an argument of a call fits a parameter: what `Classify` says, and besides that a slice goes where the
    /// view of one is asked (see `ClassifySliceView`). This one is only for calls: an assignment does not do it.
    /// </summary>
    public static ArgumentMatch ClassifyArgument(
        ITypeRef? argument, IrExpression argumentNode, ITypeRef? parameter,
        ImplicitConversions conversions, AccessContext accessor)
    {
        var match = Classify(argument, argumentNode, parameter, conversions, accessor);
        return match.Kind == MatchKind.None && argument != null && parameter != null ? ClassifySliceView(argument, parameter) : match;
    }

    /// <summary>
    /// A `[]T` goes where `Span(T)`, `ReadOnlySpan(T)`, `Memory(T)` or `ReadOnlyMemory(T)` of .NET is asked (the slice is a
    /// `Memory(T)` in the backend). An array `[N]T` does not: it is a buffer of the stack.
    /// </summary>
    private static ArgumentMatch ClassifySliceView(ITypeRef argument, ITypeRef parameter)
    {
        if (argument is not SliceTypeRef { Element: var element }) return ArgumentMatch.None;
        if (parameter is not StructTypeReference { Struct: GenericInstance { Definition: DotnetStruct definition, Arguments: [var written] } }) return ArgumentMatch.None;
        if (definition.TypeReference.FullName is not ("System.Span`1" or "System.ReadOnlySpan`1" or "System.Memory`1" or "System.ReadOnlyMemory`1"))
            return ArgumentMatch.None;

        return TypeExpressions.Raw(written) is { } wanted && SameType(wanted, element) ? Convert(SliceView) : ArgumentMatch.None;
    }

    /// <summary>A number: a sized integer, a float, or a literal of either.</summary>
    public static bool IsNumeric(ITypeRef type) =>
        IsInteger(type) || TryFloat(type, out _) || type is ComptimeIntegerTypeRef or ComptimeDecimalTypeRef;

    public static bool IsInteger(ITypeRef type) =>
        TryInteger(type, out _, out _) || TryNatural(type, out _) || TryNative(type, out _);

    /// <summary>
    /// The type both operands of an operator are brought to: the same type; the type of a literal's partner
    /// (an integer literal goes with an integer, a decimal one with a float); the larger one when the other
    /// widens into it without loss. Null if they do not meet without `as`.
    /// </summary>
    public static ITypeRef? Common(ITypeRef a, ITypeRef b)
    {
        if (a.Equals(b)) return a;

        if (a is ComptimeIntegerTypeRef) return b is ComptimeDecimalTypeRef || IsInteger(b) ? b : null;
        if (b is ComptimeIntegerTypeRef) return a is ComptimeDecimalTypeRef || IsInteger(a) ? a : null;
        if (a is ComptimeDecimalTypeRef) return TryFloat(b, out _) ? b : null;
        if (b is ComptimeDecimalTypeRef) return TryFloat(a, out _) ? a : null;

        if (ClassifyWidening(a, b).Kind != MatchKind.None) return b;
        if (ClassifyWidening(b, a).Kind != MatchKind.None) return a;
        return null;
    }

    /// <summary>How an integer literal fits a type; None if the type is not an integer or the value does not fit.</summary>
    public static ArgumentMatch ClassifyLiteral(IrExpression literal, ITypeRef parameter)
    {
        if (literal is not IrComptimeInt { BigInteger: var value })
            return ArgumentMatch.None;

        if (TryInteger(parameter, out var signed, out var bits))
            return Fits(value, signed, bits) ? Convert(LiteralToSizedInteger) : ArgumentMatch.None;

        if (TryNatural(parameter, out signed))
            return signed || value >= 0 ? Convert(signed ? LiteralToInt : LiteralToUint) : ArgumentMatch.None;

        if (TryNative(parameter, out signed))
            return signed || value >= 0 ? Convert(LiteralToSizedInteger) : ArgumentMatch.None;

        return ArgumentMatch.None;
    }

    /// <summary>
    /// A reference to a struct goes where a reference to something it extends is expected; the more steps
    /// up, the more it costs. A struct by value is never cut down to what it extends.
    /// </summary>
    private static ArgumentMatch ClassifyUpcast(ITypeRef argument, ITypeRef parameter)
    {
        if (!CompilerStd.Types.IsRef(argument, out var derivedType) || derivedType is not StructTypeReference { Struct: IMemberContainer derived }
            || !CompilerStd.Types.IsRef(parameter, out var targetType) || targetType is not StructTypeReference { Struct: var target })
            return ArgumentMatch.None;

        HashSet<IMemberContainer> visited = [];
        var steps = 0;
        for (var current = derived.Base; current != null && visited.Add(current); current = current.Base)
        {
            steps++;
            if (ReferenceEquals(current, target)) return Convert(Upcast + steps);
        }

        return ArgumentMatch.None;
    }

    /// <summary>
    /// A struct goes where a protocol it implements is expected (also one the protocols it implements extend): by value it is
    /// boxed in .NET, a class is the same object. `*S` goes as well.
    /// </summary>
    private static ArgumentMatch ClassifyProtocol(ITypeRef argument, ITypeRef parameter)
    {
        var written = CompilerStd.Types.IsRef(argument, out var inner) ? inner : argument;
        var wanted = CompilerStd.Types.IsRef(parameter, out var wantedInner) ? wantedInner : parameter;

        if (written is not StructTypeReference { Struct: TqStruct derived }) return ArgumentMatch.None;

        IMemberContainer? target = wanted switch
        {
            StructTypeReference { Struct: IMemberContainer container } => container,
            ProtocolTypeReference { Protocol: IMemberContainer container } => container,
            _ => null,
        };

        if (target is not IObjectType { IsInterface: true }) return ArgumentMatch.None;

        foreach (var (reached, steps) in Ancestry.Reachable(derived, StructSemantics.IsReference(derived)))
        {
            if (ReferenceEquals(reached, target)) return Convert(Upcast + steps);
        }

        return ArgumentMatch.None;
    }

    /// <summary>
    /// A class or interface of .NET goes where an interface it implements is expected, also a generic one:
    /// `List(i32)` goes where `IEnumerable(i32)` is, because `List(T)` implements `IEnumerable(T)` and the `T` is `i32`.
    /// It is the same object, so nothing is converted. A struct of .NET by value is not here: it would have to be boxed.
    /// </summary>
    private static ArgumentMatch ClassifyDotnetInterface(ITypeRef argument, ITypeRef parameter)
    {
        var written = Unwrap(argument);
        var wanted = Unwrap(parameter);

        if (DefinitionOf(wanted) is not IObjectType { IsInterface: true } || DefinitionOf(written) is not (DotnetClass or DotnetInterface))
            return ArgumentMatch.None;

        HashSet<ITypeRef> visited = [written];
        Queue<(ITypeRef Type, int Steps)> pending = [];
        pending.Enqueue((written, 0));

        while (pending.TryDequeue(out var current))
        {
            foreach (var parent in ParentsOf(current.Type))
            {
                if (!visited.Add(parent)) continue;
                if (SameType(parent, wanted)) return Convert(Upcast + current.Steps + 1);
                pending.Enqueue((parent, current.Steps + 1));
            }
        }

        return ArgumentMatch.None;
    }

    /// <summary>
    /// The same type, also when two applications of a generic type were not given the same entry of the instance table
    /// (`IEnumerable(i32)` written by hand and the one found in the metadata of .NET): same definition, same arguments.
    /// </summary>
    private static bool SameType(ITypeRef a, ITypeRef b)
    {
        if (a.Equals(b)) return true;

        if (a is not StructTypeReference { Struct: GenericInstance first } || b is not StructTypeReference { Struct: GenericInstance second })
            return false;
        if (!ReferenceEquals(first.Definition, second.Definition) || first.Arguments.Length != second.Arguments.Length) return false;

        for (var i = 0; i < first.Arguments.Length; i++)
        {
            var left = TypeExpressions.Raw(first.Arguments[i]);
            var right = TypeExpressions.Raw(second.Arguments[i]);
            if (left == null || right == null || !SameType(left, right)) return false;
        }

        return true;
    }

    private static ITypeRef Unwrap(ITypeRef type) => CompilerStd.Types.IsRef(type, out var inner) ? inner : type;

    /// <summary>The type constructor or type behind a type: `List` for `List(i32)`.</summary>
    private static object? DefinitionOf(ITypeRef type) => type switch
    {
        StructTypeReference { Struct: GenericInstance { Definition: var definition } } => definition,
        StructTypeReference { Struct: var structure } => structure,
        ProtocolTypeReference { Protocol: var protocol } => protocol,
        _ => null,
    };

    /// <summary>The base and interfaces of a type of .NET, with the arguments of the type in place of its own parameters.</summary>
    private static IEnumerable<ITypeRef> ParentsOf(ITypeRef type)
    {
        IEnumerable<ITypeRef?> declared = DefinitionOf(type) switch
        {
            DotnetClass dotnet => [dotnet.BaseType, ..dotnet.Implements],
            DotnetInterface dotnet => dotnet.ParentInterfaces,
            _ => [],
        };

        var bound = TypeChecker.BindingsOf(type);
        foreach (var parent in declared)
        {
            if (parent == null || TypeChecker.Substitute(parent, bound) is not { } substituted) continue;
            yield return Unwrap(substituted);
        }
    }

    private static ArgumentMatch ClassifyWidening(ITypeRef argument, ITypeRef parameter)
    {
        if (TryInteger(argument, out var fromSigned, out var fromBits) && TryInteger(parameter, out var toSigned, out var toBits))
        {
            // Same signedness into a larger type, or unsigned into a strictly larger signed type.
            var lossless = toBits > fromBits && (fromSigned == toSigned || (!fromSigned && toSigned));
            return lossless ? Convert(Widening + toBits - fromBits) : ArgumentMatch.None;
        }

        if (TryFloat(argument, out var fromFloat) && TryFloat(parameter, out var toFloat))
            return toFloat > fromFloat ? Convert(Widening + toFloat - fromFloat) : ArgumentMatch.None;

        return ArgumentMatch.None;
    }

    private static ArgumentMatch Convert(int cost) => new(MatchKind.Convert, cost);

    private static bool Fits(BigInteger integer, bool signed, int bits)
    {
        return signed
            ? integer >= -(BigInteger.One << (bits - 1)) && integer < BigInteger.One << (bits - 1)
            : integer >= 0 && integer < BigInteger.One << bits;
    }

    public static bool IsStruct(ITypeRef type, IStruct structure) =>
        type is StructTypeReference { Struct: var candidate } && ReferenceEquals(candidate, structure);

    public static bool TryInteger(ITypeRef type, out bool signed, out int bits)
    {
        signed = false;
        bits = 0;
        if (!TryInstance(type, CompilerStd.Types.Integer, out var arguments)) return false;
        if (arguments is not [IrComptimeBool isSigned, IrComptimeInt size]) return false;

        signed = isSigned.Value;
        bits = (int)size.BigInteger;
        return true;
    }

    public static bool TryNatural(ITypeRef type, out bool signed) => TrySigned(type, CompilerStd.Types.NaturalInteger, out signed);
    public static bool TryNative(ITypeRef type, out bool signed) => TrySigned(type, CompilerStd.Types.NativeInteger, out signed);

    private static bool TrySigned(ITypeRef type, IStruct definition, out bool signed)
    {
        signed = false;
        if (!TryInstance(type, definition, out var arguments)) return false;
        if (arguments is not [IrComptimeBool isSigned]) return false;

        signed = isSigned.Value;
        return true;
    }

    public static bool TryFloat(ITypeRef type, out int bits)
    {
        bits = 0;
        if (!TryInstance(type, CompilerStd.Types.Float, out var arguments)) return false;
        if (arguments is not [IrComptimeInt size]) return false;

        bits = (int)size.BigInteger;
        return true;
    }

    private static bool TryInstance(ITypeRef type, IStruct definition, out IReadOnlyList<IrExpression> arguments)
    {
        if (type is StructTypeReference { Struct: GenericInstance instance } && ReferenceEquals(instance.Definition, definition))
        {
            arguments = instance.Arguments;
            return true;
        }

        arguments = [];
        return false;
    }
}
