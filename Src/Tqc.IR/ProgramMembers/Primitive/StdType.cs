using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.IR.ProgramMembers.Primitive;

/// <summary>
/// `Std.Type`: what is asked about a type. The functions are intrinsic: they have no body, the backend knows what they stand for and
/// lowers a call to them, because the answer is the backend's (the size of a type is what it gives it in the target, and `iptr` has
/// more than one).
/// </summary>
public static class StdType
{
    public const string NamespaceName = "Type";

    /// <summary>`SizeOf(type T) uint`: the size of `T` in bytes.</summary>
    public static readonly TqFunction SizeOf = Declare("SizeOf");

    /// <summary>`BitSizeOf(type T) uint`: the size of `T` in bits.</summary>
    public static readonly TqFunction BitSizeOf = Declare("BitSizeOf");

    /// <summary>The functions, each in its group of overloads, as the members of the namespace.</summary>
    public static IEnumerable<IMember> Members => [Group(SizeOf), Group(BitSizeOf)];

    public static bool IsIntrinsic(TqFunction function) => ReferenceEquals(function, SizeOf) || ReferenceEquals(function, BitSizeOf);

    private static TqFunction Declare(string name) => new(name, default)
    {
        Access = new MemberAccess(Visibility.Public, [], [], $"{CompilerStd.ModuleName}.{NamespaceName}"),
        Parameters = [new TqParameter(new IrReference(default, new TypeMetaReference(CompilerStd.Types.Type)), "T")],
        ReturnType = new IrReference(default, CompilerStd.Types.Natural(false).GetReference()),
    };

    private static TqFunctionGroup Group(TqFunction function) => new(function.Name) { Functions = [function] };
}
