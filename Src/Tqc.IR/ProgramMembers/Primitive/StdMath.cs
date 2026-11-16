using System.Collections.Immutable;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.IR.ProgramMembers.Primitive;

/// <summary>
/// `Std.Math.Operator`: the operators a function can take over with `@overrideOperator`.
/// A case has the name of the operator in <see cref="BinaryOperator"/> or <see cref="UnaryOperator"/>.
/// </summary>
public static class StdMath
{
    public const string NamespaceName = "Math";
    public static readonly TqTypedef Operator = BuildOperator();

    private static TqTypedef BuildOperator()
    {
        var access = new MemberAccess(Visibility.Public, [], [], $"{CompilerStd.ModuleName}.{NamespaceName}");

        var cases = Enum.GetNames<BinaryOperator>().Concat(Enum.GetNames<UnaryOperator>())
            .Select(name => new TqNamedCase(name, null, default) { Access = access })
            .ToList();

        var typedef = new TqTypedef("Operator", default)
        {
            Access = access,
            Members = cases.ToImmutableDictionary(entry => entry.Name, entry => (IMember)entry),
        };

        foreach (var entry in cases) entry.Typedef = typedef;
        return typedef;
    }
}
