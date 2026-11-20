using System.Numerics;
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
        IrComptimeInt integer => (decimal)integer.BigInteger,
        IrComptimeDecimal number => number.Value,
        _ => throw new InvalidOperationException("Not a number"),
    };

    /// <summary>
    /// Integers have no size while compiling, so nothing can overflow: `+%` and `+|` are a plain `+` (the size comes
    /// when the value meets a type, and then it must fit).
    /// </summary>
    private static IrExpression? CombineIntegers(IrBinary binary, BigInteger a, BigInteger b, ref string? error)
    {
        var location = binary.Location;

        switch (binary.Operator)
        {
            case BinaryOperator.Add or BinaryOperator.AddWrap or BinaryOperator.AddOnBounds: return new IrComptimeInt(location, a + b);
            case BinaryOperator.Sub or BinaryOperator.SubWrap or BinaryOperator.SubOnBounds: return new IrComptimeInt(location, a - b);
            case BinaryOperator.Mul or BinaryOperator.MulWrap or BinaryOperator.MulOnBounds: return new IrComptimeInt(location, a * b);

            case BinaryOperator.Pow or BinaryOperator.PowWrap or BinaryOperator.PowOnBounds:
                if (b < 0)
                {
                    error = "The exponent of an integer power must not be negative";
                    return null;
                }

                return Power(a, b, ref error) is { } power ? new IrComptimeInt(location, power) : null;

            case BinaryOperator.Div or BinaryOperator.DivFloor or BinaryOperator.DivCeil or BinaryOperator.Rest:
            {
                if (b.IsZero)
                {
                    error = "Division by zero while compiling";
                    return null;
                }

                var quotient = BigInteger.DivRem(a, b, out var rest);
                var differentSigns = rest.Sign != 0 && (rest.Sign < 0) != (b.Sign < 0);

                return new IrComptimeInt(location, binary.Operator switch
                {
                    BinaryOperator.Div => quotient,
                    BinaryOperator.DivFloor => differentSigns ? quotient - 1 : quotient,
                    BinaryOperator.DivCeil => rest.Sign != 0 && !differentSigns ? quotient + 1 : quotient,
                    _ => rest,
                });
            }

            default:
                return Compare(binary, a, b);
        }
    }

    /// <summary>A whole power. The exponent is limited so that a mistake does not eat all the memory.</summary>
    private static BigInteger? Power(BigInteger value, BigInteger exponent, ref string? error)
    {
        if (exponent.IsZero) return 1;
        if (value == 0 || value == 1) return value;
        if (value == -1) return exponent.IsEven ? 1 : -1;

        if (exponent > 100_000)
        {
            error = "The exponent of the power is too large to be evaluated while compiling";
            return null;
        }

        return BigInteger.Pow(value, (int)exponent);
    }

    private static IrExpression? CombineDecimals(IrBinary binary, decimal a, decimal b, ref string? error)
    {
        var location = binary.Location;

        switch (binary.Operator)
        {
            case BinaryOperator.Add: return new IrComptimeDecimal(location, a + b);
            case BinaryOperator.Sub: return new IrComptimeDecimal(location, a - b);
            case BinaryOperator.Mul: return new IrComptimeDecimal(location, a * b);

            case BinaryOperator.Pow:
            {
                var result = Math.Pow((double)a, (double)b);
                if (double.IsNaN(result) || Math.Abs(result) > (double)decimal.MaxValue)
                {
                    error = "The result of the power does not fit in the numbers of compile time";
                    return null;
                }

                return new IrComptimeDecimal(location, (decimal)result);
            }

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

    private static IrExpression? Compare<T>(IrBinary binary, T a, T b) where T : IComparisonOperators<T, T, bool> => binary.Operator switch
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
