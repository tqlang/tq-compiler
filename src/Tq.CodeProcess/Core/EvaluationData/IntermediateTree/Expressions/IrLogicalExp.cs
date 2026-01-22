using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrLogicalExp(
    BinaryExpressionNode origin,
    IrLogicalExp.Operators ope,
    IrExpression left,
    IrExpression right) : IrExpression(origin)
{
    public override TypeReference Type => new BooleanTypeReference();

    public Operators Operator { get; set; } = ope;
    public IrExpression Left { get; set; } = left;
    public IrExpression Right { get; set; } = right;

    public override string ToString() => $"{Operator.ToString().ToLower()} {Left}, {Right}";
    
    public enum Operators
    {
        And,
        Or,
    }
}
