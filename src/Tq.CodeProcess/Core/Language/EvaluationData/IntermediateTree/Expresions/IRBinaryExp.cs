using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expresions;

public class IRBinaryExp(
    BinaryExpressionNode origin,
    IRBinaryExp.Operators ope,
    IrExpression left,
    IrExpression right) : IrExpression(origin)
{
    public TypeReference ResultType = null!;

    public Operators Operator { get; set; } = ope;
    public IrExpression Left { get; set; } = left;
    public IrExpression Right { get; set; } = right;

    public override TypeReference Type => ResultType;

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"({Operator.ToString().ToLower()}");
        sb.AppendLine(Left.ToString().TabAll());
        sb.Append(Right.ToString().TabAll());
        sb.Append(')');
        
        return sb.ToString();
    }
    
    public enum Operators
    {
        Add,
        AddWarpAround,
        AddOnBounds,
        Subtract,
        SubtractWarpAround,
        SubtractOnBounds,
        Multiply,
        Divide,
        DivideFloor,
        DivideCeil,
        Reminder,
        
        BitwiseAnd,
        BitwiseOr,
        BitwiseXor,
        LeftShift,
        RightShift,
        
        LogicalAnd,
        LogicalOr,
    }
}
