using System.Collections;
using System.Reflection;
using Tqc.Core;
using Tqc.IR.Nodes;

namespace Tqc.Semantics;

/// <summary>
/// What depends on the size of `int` and `uint`. When the target gives them a single size the code is checked once, with that size.
/// When it gives several candidates (see `TargetAbi`), the code is checked once for each, as if the target had only that one,
/// and the result is the code itself if it came out the same, or an <see cref="IrNaturalChoice"/> with one version for each size
/// if it did not. The unit that is repeated is the expression, or the statement when the conversion is part of it (`return`);
/// a block is never repeated as a whole, only the expressions and statements in it.
/// </summary>
internal sealed partial class TypeChecker
{
    /// <summary>The errors of the size being tried; null when the errors are reported at once.</summary>
    private List<(string Message, SourceLocation Location)>? _captured;

    /// <summary>True while the code is being checked for one size, so what is inside does not try them again.</summary>
    private bool _insideCandidate;

    private IrExpression VisitPerSize(IrExpression expression, Expectation? expectation = null) =>
        PerNaturalSize(
            expression.Location,
            () => expectation != null ? VisitExpecting(expression, expectation.Value) : Visit(expression),
            (location, versions) =>
            {
                var choice = new IrNaturalChoice(location, [..versions.Select(version => new IrNaturalVariant(version.Bits, version.Node))]);
                if (types.Get(versions[0].Node) is { } type) types.Set(choice, type);
                return choice;
            },
            checkType: true);

    private IrStatement CheckPerSize(IrStatement statement, Func<IrStatement> check) =>
        PerNaturalSize(
            statement.Location,
            check,
            (location, versions) => new IrNaturalChoiceStatement(
                location, [..versions.Select(version => new IrNaturalStatementVariant(version.Bits, version.Node))]),
            checkType: false);

    private TNode PerNaturalSize<TNode>(
        SourceLocation location, Func<TNode> check, Func<SourceLocation, (int Bits, TNode Node)[], TNode> choose, bool checkType)
        where TNode : IrNode
    {
        var abi = conversions.Abi;
        if (abi.NaturalBits.Length < 2 || _insideCandidate) return check();

        var runs = new List<(int Bits, TNode Node, List<(string Message, SourceLocation Location)> Errors)>();
        _insideCandidate = true;
        try
        {
            foreach (var bits in abi.NaturalBits)
            {
                conversions.Abi = abi with { NaturalBits = [bits] };
                var errors = new List<(string, SourceLocation)>();
                _captured = errors;
                var node = check();
                runs.Add((bits, node, errors));
            }
        }
        finally
        {
            conversions.Abi = abi;
            _captured = null;
            _insideCandidate = false;
        }

        ReportPerSize(runs.Select(run => (run.Bits, run.Errors)).ToList());

        if (runs.All(run => SameTree(run.Node, runs[0].Node))) return runs[0].Node;

        var versions = runs.Select(run => (run.Bits, run.Node)).ToArray();
        if (checkType)
        {
            var first = types.Get(runs[0].Node);
            if (runs.Any(run => !Equals(types.Get(run.Node), first)))
            {
                Error("The type of this expression depends on the size of 'int' in the target: "
                      + string.Join(", ", runs.Select(run => $"{run.Bits} bits gives '{(types.Get(run.Node) is { } type ? TypeFormatter.Format(type) : "?")}'")),
                    location);
            }
        }

        return choose(location, versions);
    }

    /// <summary>
    /// An error that every size gives is reported once; one that only some give says which: the line, the sizes (the target) and the reason.
    /// </summary>
    private void ReportPerSize(List<(int Bits, List<(string Message, SourceLocation Location)> Errors)> runs)
    {
        var all = runs.SelectMany(run => run.Errors.Select(error => (run.Bits, error.Message, error.Location)));
        foreach (var group in all.GroupBy(error => (error.Message, error.Location)))
        {
            var sizes = group.Select(error => error.Bits).Distinct().ToList();
            var message = sizes.Count == runs.Count
                ? group.Key.Message
                : $"{group.Key.Message} (when 'int' has {string.Join(" or ", sizes)} bits)";
            if (_silenced == 0) diagnostics.ReportError(new TypeError(message, group.Key.Location));
        }
    }

    /// <summary>Whether two checked trees are the same code with the same types.</summary>
    private bool SameTree(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;

        if (a is IrNode left && b is IrNode right)
        {
            if (a.GetType() != b.GetType() || !Equals(types.Get(left), types.Get(right))) return false;

            foreach (var property in a.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length != 0 || property.Name == "EqualityContract") continue;
                if (!SameTree(property.GetValue(a), property.GetValue(b))) return false;
            }

            return true;
        }

        if (a is IEnumerable first && b is IEnumerable second && a is not string)
        {
            var one = first.Cast<object?>().ToList();
            var other = second.Cast<object?>().ToList();
            return one.Count == other.Count && one.Zip(other).All(pair => SameTree(pair.First, pair.Second));
        }

        return a.Equals(b);
    }
}
