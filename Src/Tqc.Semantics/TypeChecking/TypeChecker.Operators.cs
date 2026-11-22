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

        if (binary.Operator == BinaryOperator.Coalesce) return VisitCoalesce(visited, left, right);

        var isComparison = binary.Operator >= BinaryOperator.Equal;
        ITypeRef? resultOf(ITypeRef common) => isComparison ? BoolType() : common;

        // Two number literals are one literal: `2 ** 128 - 1` is a value of compile time (they have no size, so it
        // does not overflow), and it only has to fit the type it meets.
        if (left is IrComptimeInt or IrComptimeDecimal && right is IrComptimeInt or IrComptimeDecimal
            && TypeOf(left) is ComptimeIntegerTypeRef or ComptimeDecimalTypeRef && TypeOf(right) is ComptimeIntegerTypeRef or ComptimeDecimalTypeRef)
        {
            var folded = ComptimeEvaluator.TryEvaluate(visited, out var failure);
            if (failure != null)
            {
                Error(failure, binary.Location);
                return (visited, isComparison ? BoolType() : null);
            }

            if (folded is IrComptimeInt or IrComptimeDecimal) return Compute(folded);
        }

        var leftType = TypeOf(left);
        var rightType = TypeOf(right);
        if (leftType == null || rightType == null) return (visited, isComparison ? BoolType() : null);

        // `x == null` and `x != null`: only a `?T` can be null.
        if (binary.Operator is BinaryOperator.Equal or BinaryOperator.NotEqual && (left is IrNull || right is IrNull))
        {
            var other = left is IrNull ? rightType : leftType;
            if (!(left is IrNull && right is IrNull) && !CompilerStd.Types.IsNullable(other, out _))
                Error($"Only a '?T' can be compared with null, not '{TypeFormatter.Format(other)}'", binary.Location);

            return (visited, BoolType());
        }

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

    /// <summary>`a ?? b`: `a` is a `?T` and `b` a `T`; the value is a `T`.</summary>
    private (IrExpression, ITypeRef?) VisitCoalesce(IrBinary visited, IrExpression left, IrExpression right)
    {
        var leftType = TypeOf(left);
        if (leftType == null) return (visited, null);

        if (!CompilerStd.Types.IsNullable(leftType, out var inner))
        {
            Error($"The left side of '??' has to be a '?T', not '{TypeFormatter.Format(leftType)}'", left.Location);
            return (visited, null);
        }

        var value = Coerce(right, inner, TypeExpressionOf(inner, right.Location), "the right side of '??'");
        return (visited with { Right = value }, inner);
    }

    // ---- Unary operators

    /// <summary>
    /// `try x` (a `!T` is a `T`, or the error leaves the function, which has to return a `!U` itself), `x.ignored()` / `x!` (a `!T` is a `?T`
    /// that is null if it failed) and `x.unwrap()` / `x.value` (a `?T` is a `T`; null is an error).
    /// </summary>
    private (IrExpression, ITypeRef?) VisitWrapperOperator(IrUnary unary, IrExpression operand)
    {
        var visited = unary with { Operand = operand };
        var type = TypeOf(operand);
        if (type == null) return (visited, null);

        var shown = TypeFormatter.Format(type);
        var failable = CompilerStd.Types.IsFailable(type, out var failed);
        var nullable = CompilerStd.Types.IsNullable(type, out var maybe);
        var voidResult = failable && failed != null && ConversionRules.IsStruct(failed, CompilerStd.Types.Void);

        switch (unary.Operator)
        {
            case UnaryOperator.Try:
            {
                if (!failable)
                {
                    Error($"'try' takes a '!T', not '{shown}'", unary.Location);
                    return (visited, null);
                }

                var returns = _function is { ReturnType: { } returnExpression } and not (TqConstructor or TqDestructor)
                    ? TypeFromExpression(returnExpression)
                    : null;
                if (_function == null || returns == null || !CompilerStd.Types.IsFailable(returns, out _))
                    Error("'try' returns the error from the function, so the function has to return a '!T'", unary.Location);

                return (visited, voidResult ? failed : StructSemantics.ValueTypeOf(failed));
            }

            case UnaryOperator.Ignored:
            {
                if (!failable)
                {
                    Error($"'ignored' and '!' take a '!T', not '{shown}'", unary.Location);
                    return (visited, null);
                }

                // `!void` has nothing to keep: ignoring it is nothing.
                if (voidResult) return (visited, failed);
                return (visited, failed != null ? NullableOf(StructSemantics.ValueTypeOf(failed), unary.Location) : null);
            }

            default:
                if (!nullable)
                {
                    Error($"'unwrap' and 'value' take a '?T', not '{shown}'", unary.Location);
                    return (visited, null);
                }

                return (visited, maybe);
        }
    }

    /// <summary>`?T` for a `T`.</summary>
    private ITypeRef? NullableOf(ITypeRef inner, SourceLocation location)
    {
        var expression = TypeExpressionOf(inner, location);
        if (expression == null) return null;

        var instance = GenericInstanceTable.Shared.GetOrAdd(CompilerStd.Types.Nullable, [expression]);
        return TypeFromExpression(new IrReference(location, instance.GetReference()));
    }

    private (IrExpression, ITypeRef?) VisitUnary(IrUnary unary)
    {
        var operand = Visit(unary.Operand);
        var visited = unary with { Operand = operand };

        if (unary is { IsPostfix: false, Operator: UnaryOperator.Negate } && operand is IrComptimeInt or IrComptimeDecimal
            && TypeOf(operand) is ComptimeIntegerTypeRef or ComptimeDecimalTypeRef
            && ComptimeEvaluator.TryEvaluate(visited, out _) is { } negated)
            return Compute(negated);

        if (unary.Operator is UnaryOperator.Try or UnaryOperator.Ignored or UnaryOperator.Unwrap)
            return VisitWrapperOperator(unary, operand);

        var type = TypeOf(operand);

        if (unary.Operator is UnaryOperator.Increment or UnaryOperator.Decrement && ConstantNameOf(operand) is { } constant)
            Error($"'{constant}' is a constant and cannot be changed", unary.Location);

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

        if (ElementOf(targetType) is { } elementType)
        {
            // `a[2 .. 3]`: a slice of that part of the array, which refers to the same memory.
            if (indices is [IrRange range])
            {
                CheckRangeBounds(range, targetType);
                return (visited, new SliceTypeRef(elementType));
            }

            CheckPositionIndices(indices);
            CheckBounds(indices, targetType);
            return (visited, elementType);
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
            $"indexer of {TypeFormatter.Format(targetType)}", "overload", [..getters.Select(ToOverloadOf)], indices, index.Location,
            BindingsOf(TypeOf(target)));
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

            if (ElementOf(targetType) is { } sliceElement)
            {
                if (indices is [IrRange])
                    Error("A range cannot be assigned: write the elements one by one", index.Location);

                CheckPositionIndices(indices);
                CheckBounds(indices, targetType);
                element = sliceElement;
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
            $"indexer of {TypeFormatter.Format(targetType)}", "overload", [..setters.Select(ToOverloadOf)], arguments, index.Location,
            BindingsOf(TypeOf(target)));
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
    /// the attribute (whatever their access: `@private` still serves `a[i]`), and the accessors of the indexed properties of a .NET type.
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
                    ));

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
    /// <summary>The type of the elements of an array or a slice; null for any other type.</summary>
    private static ITypeRef? ElementOf(ITypeRef? type) => type switch
    {
        SliceTypeRef slice => slice.Element,
        ArrayTypeRef array => array.Element,
        _ => null,
    };

    /// <summary>A literal index that is past the end of an array, which has a size when compiling.</summary>
    private void CheckBounds(IrExpression[] indices, ITypeRef? target)
    {
        if (target is not ArrayTypeRef array) return;

        foreach (var index in indices)
        {
            if (index is IrComptimeInt { BigInteger: var value } && (value < 0 || value >= array.Length))
                Error($"The index {value} is out of the bounds of '{TypeFormatter.Format(array)}'", index.Location);
        }
    }

    private void CheckRangeBounds(IrRange range, ITypeRef? target) => CheckBounds([range.From, range.To], target);

    private void CheckPositionIndices(IrExpression[] indices)
    {
        foreach (var index in indices)
        {
            if (index is IrRange)
            {
                Error("A range must be the only index", index.Location);
                continue;
            }

            var type = TypeOf(index);
            if (type != null && !ConversionRules.IsInteger(type) && type is not ComptimeIntegerTypeRef)
                Error($"An index must be an integer, not '{TypeFormatter.Format(type)}'", index.Location);
        }
    }
}
