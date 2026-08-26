using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

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
