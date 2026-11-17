using Tqc.Core;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal sealed partial class TypeChecker
{
    // ---- Binary operators

    /// <summary>
    /// The operands are brought to a common type, and the operator must exist for it: numbers take every arithmetic
    /// and comparison operator, strings only operate with strings (`+` and comparisons), and anything of the
    /// same type can be tested for equality. A type can take over an operator with `@overrideOperator`.
    /// </summary>
    private (IrExpression, ITypeRef?) VisitBinary(IrBinary binary)
    {
        var left = Visit(binary.Left);
        var right = Visit(binary.Right);
        var visited = binary with { Left = left, Right = right };

        var isComparison = binary.Operator >= BinaryOperator.Equal;
        ITypeRef? resultOf(ITypeRef common) => isComparison ? BoolType() : common;

        var leftType = TypeOf(left);
        var rightType = TypeOf(right);
        if (leftType == null || rightType == null) return (visited, isComparison ? BoolType() : null);

        var common = ConversionRules.Common(leftType, rightType);
        if (common != null && BinaryOperatorExists(binary.Operator, common))
        {
            CheckLiteralFits(left, right, leftType, rightType, common);
            return (visited, resultOf(common));
        }

        if (TryOverloadedOperator(binary.Operator.ToString(), binary.Location, [left, right], visited) is { } overloaded)
            return overloaded;

        if (common == null)
            Error($"The operator '{binary.Operator}' cannot be applied to '{TypeFormatter.Format(leftType)}' and " +
                  $"'{TypeFormatter.Format(rightType)}'; convert one of them with `as`", binary.Location);
        else
            Error($"The operator '{binary.Operator}' does not exist for '{TypeFormatter.Format(common)}'", binary.Location);

        return (visited, isComparison ? BoolType() : null);
    }

    private static bool BinaryOperatorExists(BinaryOperator @operator, ITypeRef type)
    {
        var isString = ConversionRules.IsStruct(type, CompilerStd.Types.String);

        return @operator switch
        {
            BinaryOperator.Equal or BinaryOperator.ExactEqual
                or BinaryOperator.NotEqual or BinaryOperator.NotExactEqual => true,
            BinaryOperator.Less or BinaryOperator.Greater
                or BinaryOperator.LessEqual or BinaryOperator.GreaterEqual =>
                ConversionRules.IsNumeric(type) || isString || ConversionRules.IsStruct(type, CompilerStd.Types.Char),
            BinaryOperator.Add => ConversionRules.IsNumeric(type) || isString,
            _ => ConversionRules.IsNumeric(type),
        };
    }

    /// <summary>A literal next to a sized number must fit in it: `x + 300` with an `i8` `x` is an error.</summary>
    private void CheckLiteralFits(
        IrExpression left, IrExpression right, ITypeRef leftType, ITypeRef rightType, ITypeRef common)
    {
        if (common is ComptimeIntegerTypeRef or ComptimeDecimalTypeRef) return;

        foreach (var (operand, type) in new[] { (left, leftType), (right, rightType) })
        {
            if (operand is not IrComptimeInt literal || type is not ComptimeIntegerTypeRef) continue;

            if (ConversionRules.ClassifyLiteral(literal, common).Kind == MatchKind.None)
                Error($"The literal {literal.BigInteger} does not fit in '{TypeFormatter.Format(common)}'", literal.Location);
        }
    }

    // ---- Unary operators

    private (IrExpression, ITypeRef?) VisitUnary(IrUnary unary)
    {
        var operand = Visit(unary.Operand);
        var visited = unary with { Operand = operand };
        var type = TypeOf(operand);

        if (type == null) return (visited, unary.Operator == UnaryOperator.Not ? BoolType() : null);

        var exists = unary.Operator == UnaryOperator.Not ? IsBool(type) : ConversionRules.IsNumeric(type);
        if (exists) return (visited, unary.Operator == UnaryOperator.Not ? BoolType() : type);

        if (TryOverloadedOperator(unary.Operator.ToString(), unary.Location, [operand], visited) is { } overloaded)
            return overloaded;

        Error($"The operator '{unary.Operator}' does not exist for '{TypeFormatter.Format(type)}'", unary.Location);
        return (visited, unary.Operator == UnaryOperator.Not ? BoolType() : null);
    }

    /// <summary>
    /// The call to the function that takes over the operator, if one is registered for it.
    /// Null when none is, so that the caller reports the operator as missing; when some are, anything that goes
    /// wrong is reported here and the unresolved expression is returned.
    /// </summary>
    private (IrExpression, ITypeRef?)? TryOverloadedOperator(
        string @operator, SourceLocation location, IrExpression[] operands, IrExpression unresolved)
    {
        var entries = operators.For(@operator)
            .Where(entry => AccessChecker.CanAccess(entry.Function, _accessor))
            .ToList();

        // A function of a struct takes the left operand as `this`, so it only applies to that struct.
        var onReceiver = entries
            .Where(entry => entry.Receiver != null && ReferenceEquals(ContainerOf(TypeOf(operands[0])), entry.Receiver))
            .ToList();
        var asFunctions = entries.Where(entry => entry.Receiver == null).ToList();

        var chosen = onReceiver.Count > 0 ? onReceiver : asFunctions;
        if (chosen.Count == 0) return null;

        var useReceiver = onReceiver.Count > 0;
        var arguments = useReceiver ? operands[1..] : operands;
        var name = $"operator {@operator}";

        var winner = ChooseOverload(name, "overload", [..chosen.Select(entry => ToOverload(entry.Function))], arguments, location);
        if (winner == null) return (unresolved, null);

        var function = new FunctionTypeReference(winner.Overload.Function);
        var converted = new IrArgumentsList(location, ConvertArguments(arguments, winner));

        IrExpression callee = useReceiver
            ? new IrAccess(location, operands[0], new IrReference(location, function))
            : new IrReference(location, function);

        var call = new IrCall(location, callee, converted);
        return (call, ReturnTypeOf(callee));
    }

    // ---- Indexers

    /// <summary>
    /// `a[i]`: slices and strings are indexed by an integer (a string gives a char); any other type needs a
    /// function marked `@indexerGet`.
    /// </summary>
    private (IrExpression, ITypeRef?) VisitIndex(IrIndex index)
    {
        var target = Visit(index.Target);
        var indices = index.Indices.Select(Visit).ToArray();
        var visited = index with { Target = target, Indices = indices };

        var targetType = TypeOf(target);
        if (targetType == null) return (visited, null);

        if (targetType is SliceTypeRef slice)
        {
            CheckPositionIndices(indices);
            return (visited, slice.Element);
        }

        if (ConversionRules.IsStruct(targetType, CompilerStd.Types.String))
        {
            CheckPositionIndices(indices);
            return (visited, new StructTypeReference(CompilerStd.Types.Char));
        }

        var container = ContainerOf(targetType);
        if (!HasIndexers(container))
        {
            if (container == null)
                Error($"'{TypeFormatter.Format(targetType)}' cannot be indexed", index.Location);
            return (visited, null);
        }

        var getters = IndexerFunctions(container, BuiltinAttribute.IndexerGet);
        if (getters.Count == 0)
        {
            Error(container is TqStruct or TqTypedef
                ? $"'{TypeFormatter.Format(targetType)}' has no function marked '@indexerGet'"
                : $"'{TypeFormatter.Format(targetType)}' has no indexer to read", index.Location);
            return (visited, null);
        }

        var winner = ChooseOverload(
            $"indexer of {TypeFormatter.Format(targetType)}", "overload", [..getters.Select(ToOverloadOf)], indices, index.Location);
        if (winner == null) return (visited, null);

        var callee = IndexerCallee(index, target, winner);
        var call = new IrCall(index.Location, callee, new IrArgumentsList(index.Location, ConvertArguments(indices, winner)));
        return (call, ReturnTypeOf(callee));
    }

    /// <summary>`a[i] = v`: a slice element is assigned directly; otherwise the `@indexerSet` function is called.</summary>
    private (IrExpression, ITypeRef?) VisitIndexAssign(IrAssign assign, IrIndex index)
    {
        var source = Visit(assign.Source);
        var target = Visit(index.Target);
        var indices = index.Indices.Select(Visit).ToArray();
        var targetType = TypeOf(target);

        var visitedIndex = index with { Target = target, Indices = indices };
        var container = ContainerOf(targetType);

        if (!HasIndexers(container))
        {
            ITypeRef? element = null;

            if (targetType is SliceTypeRef slice)
            {
                CheckPositionIndices(indices);
                element = slice.Element;
                types.Set(visitedIndex, element);
            }
            else if (targetType != null && container == null)
            {
                Error($"'{TypeFormatter.Format(targetType)}' cannot be assigned by index", index.Location);
            }

            return (assign with { Target = visitedIndex, Source = source }, element);
        }

        var setters = IndexerFunctions(container, BuiltinAttribute.IndexerSet);
        if (setters.Count == 0)
        {
            Error(container is TqStruct or TqTypedef
                ? $"'{TypeFormatter.Format(targetType!)}' has no function marked '@indexerSet'"
                : $"'{TypeFormatter.Format(targetType!)}' has no indexer to write", index.Location);
            return (assign with { Target = visitedIndex, Source = source }, null);
        }

        IrExpression[] arguments = [..indices, source];
        var winner = ChooseOverload(
            $"indexer of {TypeFormatter.Format(targetType)}", "overload", [..setters.Select(ToOverloadOf)], arguments, index.Location);
        if (winner == null) return (assign with { Target = visitedIndex, Source = source }, null);

        var callee = IndexerCallee(index, target, winner);
        var call = new IrCall(assign.Location, callee, new IrArgumentsList(assign.Location, ConvertArguments(arguments, winner)));
        return (call, ReturnTypeOf(callee));
    }

    private static IrExpression IndexerCallee(IrIndex index, IrExpression target, Candidate winner) =>
        new IrAccess(index.Location, target, new IrReference(index.Location, new FunctionTypeReference(winner.Overload.Function)));

    /// <summary>Tq structs and typedefs and .NET types declare their indexers; the other types have none.</summary>
    private static bool HasIndexers(IMemberContainer? container) =>
        container is TqStruct or TqTypedef or DotnetClass or DotnetStruct or DotnetInterface;

    private static Overload ToOverloadOf(IFunction function) => function switch
    {
        TqFunction tq => ToOverload(tq),
        DotnetMethod method => ToOverload(method),
        _ => throw new InvalidOperationException($"'{function.Name}' cannot be an indexer"),
    };

    /// <summary>
    /// The functions of the container, or of what it extends, that read or write an index: the ones that carry
    /// the attribute, and the accessors of the indexed properties of a .NET type.
    /// </summary>
    private List<IFunction> IndexerFunctions(IMemberContainer container, BuiltinAttribute kind)
    {
        List<IFunction> found = [];
        HashSet<IMemberContainer> visited = [];

        for (var current = container; current != null && visited.Add(current); current = current.Base)
        {
            foreach (var member in current.OwnMembers)
            {
                IEnumerable<TqFunction> functions = member switch
                {
                    TqFunctionGroup group => group,
                    TqFunction function => [function],
                    _ => [],
                };

                found.AddRange(functions.Where(function =>
                    function.Attributes.OfType<TqBuiltinAttribute>().Any(attribute => attribute.Kind == kind)
                    && AccessChecker.CanAccess(function, _accessor)));

                if (member is DotnetProperty { IsIndexer: true } indexer)
                {
                    var accessor = kind == BuiltinAttribute.IndexerGet ? indexer.GetMethod : indexer.SetMethod;
                    if (accessor != null) found.Add(accessor);
                }
            }
        }

        return found;
    }

    /// <summary>Any integer type is valid as a position.</summary>
    private void CheckPositionIndices(IrExpression[] indices)
    {
        foreach (var index in indices)
        {
            var type = TypeOf(index);
            if (type != null && !ConversionRules.IsInteger(type) && type is not ComptimeIntegerTypeRef)
                Error($"An index must be an integer, not '{TypeFormatter.Format(type)}'", index.Location);
        }
    }
}
