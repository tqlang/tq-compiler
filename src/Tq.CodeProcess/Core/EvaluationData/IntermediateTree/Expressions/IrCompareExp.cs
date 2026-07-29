using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrCompareExp(
    BinaryExpressionNode origin,
    IrCompareExp.Operators ope,
    IrExpression left,
    IrExpression right) : IrExpression(origin)
{
    public override ITypeReference Type => new BooleanTypeReference();

    public Operators Operator { get; set; } = ope;
    public IrExpression Left { get; set; } = left;
    public IrExpression Right { get; set; } = right;

    public override string ToString() => $"cmp_{Operator}({Left}, {Right})";
    
    public enum Operators
    {
        Equality,
        Inequality,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
    }
}
