using Tqc.Core;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// The functions marked `@overrideOperator(Std.Math.Operator.Name)`, by operator.
/// A function written inside a struct that is not `@static` has the left operand as `this`, so it takes one
/// parameter less; any other takes all the operands.
/// </summary>
internal sealed class OperatorOverloads
{
    internal sealed record Entry(TqFunction Function, IMemberContainer? Receiver);

    private readonly Dictionary<string, List<Entry>> _entries = [];

    public IReadOnlyList<Entry> For(string @operator) => _entries.TryGetValue(@operator, out var list) ? list : [];

    public static bool IsUnary(string @operator) => @operator is "Not" or "Negate" or "Increment" or "Decrement";

    /// <summary>`roots[0]` is the module being checked: only its mistakes are reported.</summary>
    public static OperatorOverloads Build(IReadOnlyList<INamespace> roots, Action<string, SourceLocation> report)
    {
        var result = new OperatorOverloads();

        for (var i = 0; i < roots.Count; i++)
        foreach (var (function, owner) in FunctionScanner.All(roots[i]))
        foreach (var attribute in function.Attributes.OfType<TqBuiltinAttribute>())
        {
            if (attribute.Kind != BuiltinAttribute.OverrideOperator) continue;
            if (attribute.Arguments is not [IrReference { Reference: FieldReference { Field: TqNamedCase entry } }]) continue;

            var receiver = owner != null && !function.IsStatic ? owner : null;
            var expected = (IsUnary(entry.Name) ? 1 : 2) - (receiver != null ? 1 : 0);

            if (function.Parameters.Length != expected || function.ReturnType == null)
            {
                if (i == 0)
                    report($"An override of '{entry.Name}' must take {expected} parameter(s) and return a value", function.SourceLocation);
                continue;
            }

            if (!result._entries.TryGetValue(entry.Name, out var list)) result._entries[entry.Name] = list = [];
            list.Add(new Entry(function, receiver));
        }

        return result;
    }
}
