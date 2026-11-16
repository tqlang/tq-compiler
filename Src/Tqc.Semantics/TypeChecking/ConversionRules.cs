using System.Numerics;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
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
/// </summary>
internal readonly record struct ArgumentMatch(MatchKind Kind, int Cost, IFunction? Via = null)
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
    private const int UserConversion = 100;
    private const int GenericParameter = 1000;

    public static ArgumentMatch Classify(
        ITypeRef? argument, IrExpression argumentNode, ITypeRef? parameter,
        ImplicitConversions conversions, AccessContext accessor)
    {
        if (argument == null || parameter == null) return ArgumentMatch.Unknown;

        var types = CompilerStd.Types;

        if (IsStruct(parameter, types.AnyType)) return new ArgumentMatch(MatchKind.Generic, GenericParameter);
        if (IsStruct(parameter, types.Type)) return IsStruct(argument, types.Type) ? ArgumentMatch.Exact : ArgumentMatch.None;

        if (argument.Equals(parameter)) return ArgumentMatch.Exact;

        if (argument is ComptimeIntegerTypeRef) return ClassifyLiteral(argumentNode, parameter);
        if (argument is ComptimeDecimalTypeRef)
            return TryFloat(parameter, out var floatBits) ? Convert(floatBits == 64 ? DecimalToF64 : DecimalToOtherFloat) : ArgumentMatch.None;

        if (argument is SliceTypeRef && parameter is SliceTypeRef { Element: var element } && IsStruct(element, types.AnyType))
            return new ArgumentMatch(MatchKind.Generic, GenericParameter);

        var widening = ClassifyWidening(argument, parameter);
        if (widening.Kind != MatchKind.None) return widening;

        var upcast = ClassifyUpcast(argument, parameter);
        if (upcast.Kind != MatchKind.None) return upcast;

        var via = conversions.Find(argument, parameter, accessor);
        return via != null ? new ArgumentMatch(MatchKind.Convert, UserConversion, via) : ArgumentMatch.None;
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
        if (literal is not IrComptimeInt { BigInteger: var value } || value != decimal.Truncate(value))
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
        if (argument is not ReferenceTypeRef { Referred: StructTypeReference { Struct: IMemberContainer derived } }
            || parameter is not ReferenceTypeRef { Referred: StructTypeReference { Struct: var target } })
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

    private static bool Fits(decimal value, bool signed, int bits)
    {
        var integer = new BigInteger(value);
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
