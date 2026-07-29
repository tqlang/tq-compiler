using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrBinaryExp(
    BinaryExpressionNode origin,
    IrBinaryExp.Operators ope,
    IrExpression left,
    IrExpression right) : IrExpression(origin)
{
    public ITypeReference ResultType = null!;

    public Operators Operator { get; set; } = ope;
    public IrExpression Left { get; set; } = left;
    public IrExpression Right { get; set; } = right;

    public override ITypeReference Type => ResultType;

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
