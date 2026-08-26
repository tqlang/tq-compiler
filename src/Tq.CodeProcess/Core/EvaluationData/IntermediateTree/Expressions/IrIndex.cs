using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrIndex(SyntaxNode origin, IrExpression value, IrExpression[] indices) : IrExpression(origin)
{
    public override ITypeReference Type => ResultType;
    public ITypeReference ResultType = null!;
    
    public IrExpression Value = value;
    public IrExpression[] Indices = indices;

    public override string ToString() => $"{Value}[{string.Join(", ", Indices)}]";
}
