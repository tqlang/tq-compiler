using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;

public class IrIndex(SyntaxNode origin, IrExpression value, IrExpression[] indices) : IrExpression(origin)
{
    public override TypeReference Type => ResultType;
    public TypeReference ResultType = null!;
    
    public IrExpression Value = value;
    public IrExpression[] Indices = indices;

    public override string ToString() => $"{Value}[{string.Join(", ", Indices)}]";
}
