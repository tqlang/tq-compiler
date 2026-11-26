namespace Tqc.IR.Nodes;

/// <summary>The suffixes mean: `Wrap` wraps around on overflow; `OnBounds` saturates at the bounds; `Floor` and `Ceil` round the division.</summary>
public enum BinaryOperator
{
    Add, AddWrap, AddOnBounds,
    Sub, SubWrap, SubOnBounds,
    Mul, MulWrap, MulOnBounds,
    Div, DivFloor, DivCeil, Rest, Pow, PowWrap, PowOnBounds,
    BitAnd, BitOr, BitXor, // `a ⋏ b`, `a ⋎ b`, `a ⊕ b`: integers
    ShiftLeft, ShiftRight, // `a << n`, `a >> n`: `n` must be less than the size of `a`; `>>` keeps the sign of a signed `a`
    ShiftLeftWrap, ShiftRightWrap, // `a <<% n`, `a >>% n`: `n` is taken modulo the size of `a`
    Coalesce, // `a ?? b`: `a` of a `?T`, or `b`
    Equal, ExactEqual, NotEqual, NotExactEqual,
    Less, Greater, LessEqual, GreaterEqual,
}

/// <summary>
/// `Try`: `try x` of a `!T` gives the `T`, or returns the error from the function. `Ignored`: `x.ignored()` and `x!`, the `!T` as a `?T` that is
/// `null` when it failed. `Unwrap`: `x.unwrap()` and `x.value`, the `T` of a `?T`. They are intrinsics: the backend reads the fields itself.
/// </summary>
public enum UnaryOperator { Not, Negate, Increment, Decrement, Nullable, Try, Ignored, Unwrap, BitNot }
