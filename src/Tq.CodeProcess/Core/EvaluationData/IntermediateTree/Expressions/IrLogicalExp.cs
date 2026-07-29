using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrLogicalExp(
    BinaryExpressionNode origin,
    IrLogicalExp.Operators ope,
    IrExpression left,
    IrExpression right) : IrExpression(origin)
{
    public override ITypeReference Type => new BooleanTypeReference();

    public Operators Operator { get; set; } = ope;
    public IrExpression Left { get; set; } = left;
    public IrExpression Right { get; set; } = right;

    public override string ToString() => $"{Left}\n\t{Operator.ToString().ToLower()} {Right}";
    
    public enum Operators
    {
        And,
        Or,
    }
}
