using System.Collections;
using System.Reflection;
using Tqc.Core;
using Tqc.IR.Nodes;
using Tqc.IR.Targets;

namespace Tqc.Semantics;

/// <summary>
/// What depends on the size of `int` and `uint`, and of `iptr` and `uptr`. When the target gives each a single size the code is checked once,
/// with those sizes. When it gives several candidates to one of them (see `TargetAbi`), the code is checked once for each combination, as if
/// the target had only that one, and the result is the code itself if it came out the same, or an <see cref="IrSizeChoice"/> with one version
/// for each combination if it did not. The unit that is repeated is the expression (with the conversion to what it is for, in a field, a default
/// value or an enum case), or the statement when the conversion is part of it (`return`); a block is never repeated as a whole, only the
/// expressions and statements in it.
/// </summary>
internal sealed partial class TypeChecker
{
    /// <summary>The errors of the sizes being tried; null when the errors are reported at once.</summary>
    private List<(string Message, SourceLocation Location)>? _captured;

    /// <summary>True while the code is being checked for one choice of sizes, so what is inside does not try them again.</summary>
    private bool _insideCandidate;

    private IrExpression VisitPerSize(IrExpression expression, Expectation? expectation = null) =>
        PerSizeValue(expression.Location, () => expectation != null ? VisitExpecting(expression, expectation.Value) : Visit(expression));

    /// <summary>An expression that is checked (and converted to what is asked of it) once for each choice of the sizes the target can have.</summary>
    private IrExpression PerSizeValue(SourceLocation location, Func<IrExpression> check) =>
        PerSize(
            location,
            check,
            (location, versions) =>
            {
                var choice = new IrSizeChoice(location, [..versions.Select(version => new IrSizeVariant(version.Sizes, version.Node))]);
                if (types.Get(versions[0].Node) is { } type) types.Set(choice, type);
                return choice;
            },
            checkType: true);

    private IrStatement CheckPerSize(IrStatement statement, Func<IrStatement> check) =>
        PerSize(
            statement.Location,
            check,
            (location, versions) => new IrSizeChoiceStatement(
                location, [..versions.Select(version => new IrSizeStatementVariant(version.Sizes, version.Node))]),
            checkType: false);

    private TNode PerSize<TNode>(
        SourceLocation location, Func<TNode> check, Func<SourceLocation, (TargetSizes Sizes, TNode Node)[], TNode> choose, bool checkType)
        where TNode : IrNode
    {
        var abi = conversions.Abi;

        // Only a size with more than one candidate is part of the decision; the other is the one the target has.
        int?[] naturals = abi.NaturalBits.Length > 1 ? [..abi.NaturalBits.Select(bits => (int?)bits)] : [null];
        int?[] natives = abi.NativeBits.Length > 1 ? [..abi.NativeBits.Select(bits => (int?)bits)] : [null];
        if ((naturals.Length == 1 && natives.Length == 1) || _insideCandidate) return check();

        var runs = new List<(TargetSizes Sizes, TNode Node, List<(string Message, SourceLocation Location)> Errors)>();
        _insideCandidate = true;
        try
        {
            foreach (var natural in naturals)
            foreach (var native in natives)
            {
                conversions.Abi = abi with
                {
                    NaturalBits = natural is { } n ? [n] : abi.NaturalBits,
                    NativeBits = native is { } m ? [m] : abi.NativeBits,
                };

                var errors = new List<(string, SourceLocation)>();
                _captured = errors;
                var node = check();
                runs.Add((new TargetSizes(natural, native), node, errors));
            }
        }
        finally
        {
            conversions.Abi = abi;
            _captured = null;
            _insideCandidate = false;
        }

        ReportPerSize(runs.Select(run => (run.Sizes, run.Errors)).ToList());

        if (runs.All(run => SameTree(run.Node, runs[0].Node, new HashSet<(object, object)>(PairComparer.Instance)))) return runs[0].Node;

        var versions = runs.Select(run => (run.Sizes, run.Node)).ToArray();
        // A version that failed has no type to compare: its error was said already.
        if (checkType && runs.All(run => run.Errors.Count == 0))
        {
            var first = types.Get(runs[0].Node);
            if (runs.Any(run => !Equals(types.Get(run.Node), first)))
            {
                Error("The type of this expression depends on the sizes of the target: "
                      + string.Join(", ", runs.Select(run => $"when {run.Sizes.Describe()} it is '{(types.Get(run.Node) is { } type ? TypeFormatter.Format(type) : "?")}'")),
                    location);
            }
        }

        return choose(location, versions);
    }

    /// <summary>
    /// An error that every choice of sizes gives is reported once; one that only some give says which: the line, the sizes (the target) and the reason.
    /// </summary>
    private void ReportPerSize(List<(TargetSizes Sizes, List<(string Message, SourceLocation Location)> Errors)> runs)
    {
        var all = runs.SelectMany(run => run.Errors.Select(error => (run.Sizes, error.Message, error.Location)));
        foreach (var group in all.GroupBy(error => (error.Message, error.Location)))
        {
            var sizes = group.Select(error => error.Sizes).Distinct().ToList();
            var message = sizes.Count == runs.Count
                ? group.Key.Message
                : $"{group.Key.Message} (when {string.Join(" or when ", sizes.Select(size => size.Describe()))})";
            if (_silenced == 0) diagnostics.ReportError(new TypeError(message, group.Key.Location));
        }
    }

    /// <summary>Whether two checked trees are the same code with the same types.</summary>
    private bool SameTree(object? a, object? b, HashSet<(object, object)> seen)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;

        // A pair that is being compared already (the nodes can point back at each other) is the same until something says otherwise.
        if (!seen.Add((a, b))) return true;

        // The nodes and the parts that are not nodes themselves (a `match` arm) are compared by what they hold: the arrays in them
        // do not compare by value.
        if (a.GetType() == b.GetType() && a.GetType().Namespace == "Tqc.IR.Nodes")
        {
            if (a is IrNode left && b is IrNode right && !Equals(types.Get(left), types.Get(right))) return false;

            foreach (var property in a.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length != 0 || property.Name == "EqualityContract") continue;
                if (!SameTree(property.GetValue(a), property.GetValue(b), seen)) return false;
            }

            return true;
        }

        if (a is IEnumerable first && b is IEnumerable second && a is not string)
        {
            var one = first.Cast<object?>().ToList();
            var other = second.Cast<object?>().ToList();
            return one.Count == other.Count && one.Zip(other).All(pair => SameTree(pair.First, pair.Second, seen));
        }

        return a.Equals(b);
    }
}

/// <summary>Pairs of objects by the identity of both.</summary>
file sealed class PairComparer : IEqualityComparer<(object, object)>
{
    public static PairComparer Instance { get; } = new();

    public bool Equals((object, object) x, (object, object) y) => ReferenceEquals(x.Item1, y.Item1) && ReferenceEquals(x.Item2, y.Item2);

    public int GetHashCode((object, object) pair) =>
        HashCode.Combine(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(pair.Item1), System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(pair.Item2));
}
