namespace Tqc.IR.Nodes;

/// <summary>The suffixes mean: `Wrap` wraps around on overflow; `OnBounds` saturates at the bounds; `Floor` and `Ceil` round the division.</summary>
public enum BinaryOperator
{
    Add, AddWrap, AddOnBounds,
    Sub, SubWrap, SubOnBounds,
    Mul, MulWrap, MulOnBounds,
    Div, DivFloor, DivCeil, Rest, Pow, PowWrap, PowOnBounds,
    Equal, ExactEqual, NotEqual, NotExactEqual,
    Less, Greater, LessEqual, GreaterEqual,
}

public enum UnaryOperator { Not, Negate, Increment, Decrement, Nullable }
