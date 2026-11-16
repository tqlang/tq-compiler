using Tqc.IR.Nodes;

namespace Tqc.Semantics;

/// <summary>
/// Evaluates what is known while compiling: literals and the operators applied to them. It is what the arguments
/// of a generic struct go through (`Integer(true, 8 * 4)` is `Integer(true, 32)`).
/// </summary>
internal static class ComptimeEvaluator
{
    /// <summary>
    /// The constant the expression is worth: an <see cref="IrComptimeInt"/>, <see cref="IrComptimeDecimal"/>,
    /// <see cref="IrComptimeBool"/> or <see cref="IrStringConstant"/>. Null when it is not a constant (a name,
    /// a call...), which is not an error: it just is not known yet. An error is only returned for an operation
    /// that can never work, such as a division by zero.
    /// </summary>
    public static IrExpression? TryEvaluate(IrExpression expression, out string? error)
    {
        error = null;

        try
        {
            return Evaluate(expression, ref error);
        }
        catch (OverflowException)
        {
            error = "The value is too large to be evaluated while compiling";
            return null;
        }
    }

    private static IrExpression? Evaluate(IrExpression expression, ref string? error)
    {
        switch (expression)
        {
            case IrComptimeInt or IrComptimeDecimal or IrComptimeBool or IrStringConstant:
                return expression;

            case IrUnary { IsPostfix: false } unary when unary.Operator is UnaryOperator.Not or UnaryOperator.Negate:
            {
                var operand = Evaluate(unary.Operand, ref error);
                return (unary.Operator, operand) switch
                {
                    (UnaryOperator.Not, IrComptimeBool boolean) => new IrComptimeBool(unary.Location, !boolean.Value),
                    (UnaryOperator.Negate, IrComptimeInt integer) => new IrComptimeInt(unary.Location, -integer.BigInteger),
                    (UnaryOperator.Negate, IrComptimeDecimal number) => new IrComptimeDecimal(unary.Location, -number.Value),
                    _ => null,
                };
            }

            case IrBinary binary:
            {
                var left = Evaluate(binary.Left, ref error);
                var right = left != null ? Evaluate(binary.Right, ref error) : null;
                return left != null && right != null ? Combine(binary, left, right, ref error) : null;
            }

            default:
                return null;
        }
    }

    private static IrExpression? Combine(IrBinary binary, IrExpression left, IrExpression right, ref string? error)
    {
        var location = binary.Location;
        var op = binary.Operator;

        switch (left, right)
        {
            case (IrStringConstant a, IrStringConstant b):
                return op switch
                {
                    BinaryOperator.Add => new IrStringConstant(location, a.Value + b.Value),
                    BinaryOperator.Equal or BinaryOperator.ExactEqual => new IrComptimeBool(location, a.Value == b.Value),
                    BinaryOperator.NotEqual or BinaryOperator.NotExactEqual => new IrComptimeBool(location, a.Value != b.Value),
                    _ => null,
                };

            case (IrComptimeBool a, IrComptimeBool b):
                return op switch
                {
                    BinaryOperator.Equal or BinaryOperator.ExactEqual => new IrComptimeBool(location, a.Value == b.Value),
                    BinaryOperator.NotEqual or BinaryOperator.NotExactEqual => new IrComptimeBool(location, a.Value != b.Value),
                    _ => null,
                };

            case (IrComptimeInt a, IrComptimeInt b):
                return CombineIntegers(binary, a.BigInteger, b.BigInteger, ref error);

            case (IrComptimeInt or IrComptimeDecimal, IrComptimeInt or IrComptimeDecimal):
                return CombineDecimals(binary, NumberOf(left), NumberOf(right), ref error);

            default:
                return null;
        }
    }

    private static decimal NumberOf(IrExpression value) => value switch
    {
        IrComptimeInt integer => integer.BigInteger,
        IrComptimeDecimal number => number.Value,
        _ => throw new InvalidOperationException("Not a number"),
    };

    private static IrExpression? CombineIntegers(IrBinary binary, decimal a, decimal b, ref string? error)
    {
        var location = binary.Location;

        switch (binary.Operator)
        {
            case BinaryOperator.Add: return new IrComptimeInt(location, a + b);
            case BinaryOperator.Sub: return new IrComptimeInt(location, a - b);
            case BinaryOperator.Mul: return new IrComptimeInt(location, a * b);

            case BinaryOperator.Div or BinaryOperator.DivFloor or BinaryOperator.DivCeil or BinaryOperator.Rest:
                if (b == 0)
                {
                    error = "Division by zero while compiling";
                    return null;
                }

                return new IrComptimeInt(location, binary.Operator switch
                {
                    BinaryOperator.Div => decimal.Truncate(a / b),
                    BinaryOperator.DivFloor => decimal.Floor(a / b),
                    BinaryOperator.DivCeil => decimal.Ceiling(a / b),
                    _ => a % b,
                });

            default:
                return Compare(binary, a, b);
        }
    }

    private static IrExpression? CombineDecimals(IrBinary binary, decimal a, decimal b, ref string? error)
    {
        var location = binary.Location;

        switch (binary.Operator)
        {
            case BinaryOperator.Add: return new IrComptimeDecimal(location, a + b);
            case BinaryOperator.Sub: return new IrComptimeDecimal(location, a - b);
            case BinaryOperator.Mul: return new IrComptimeDecimal(location, a * b);

            case BinaryOperator.Div:
                if (b == 0)
                {
                    error = "Division by zero while compiling";
                    return null;
                }

                return new IrComptimeDecimal(location, a / b);

            default:
                return Compare(binary, a, b);
        }
    }

    private static IrExpression? Compare(IrBinary binary, decimal a, decimal b) => binary.Operator switch
    {
        BinaryOperator.Equal or BinaryOperator.ExactEqual => new IrComptimeBool(binary.Location, a == b),
        BinaryOperator.NotEqual or BinaryOperator.NotExactEqual => new IrComptimeBool(binary.Location, a != b),
        BinaryOperator.Less => new IrComptimeBool(binary.Location, a < b),
        BinaryOperator.Greater => new IrComptimeBool(binary.Location, a > b),
        BinaryOperator.LessEqual => new IrComptimeBool(binary.Location, a <= b),
        BinaryOperator.GreaterEqual => new IrComptimeBool(binary.Location, a >= b),
        _ => null,
    };
}
