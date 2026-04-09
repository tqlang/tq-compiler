using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrBinaryExp(
    BinaryExpressionNode origin,
    IrBinaryExp.Operators ope,
    IrExpression left,
    IrExpression right) : IrExpression(origin)
{
    public TypeReference ResultType = null!;

    public Operators Operator { get; set; } = ope;
    public IrExpression Left { get; set; } = left;
    public IrExpression Right { get; set; } = right;

    public override TypeReference Type => ResultType;

    public override string ToString() => $"{Operator}({Left}, {Right})";
    
    public enum Operators
    {
        Add, AddWrapAround, AddOnBounds,
        Subtract, SubtractWrapAround, SubtractOnBounds,
        Multiply, MultiplyWrapAround, MultiplyOnBounds,
        Divide, DivideFloor, DivideCeil,
        Reminder,
        Pow, PowWrapAround,  PowOnBounds,
        
        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        LeftShift,
        RightShift,
    }
}
