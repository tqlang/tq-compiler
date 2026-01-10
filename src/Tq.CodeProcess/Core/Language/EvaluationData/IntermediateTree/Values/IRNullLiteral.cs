using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;

public class IRNullLiteral(SyntaxNode origin): IrExpression(origin)
{
    public override TypeReference Type => null!;
    public override string ToString() => "null";
}