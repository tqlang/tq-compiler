using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IRNullLiteral(SyntaxNode origin): IrExpression(origin)
{
    public override TypeReference Type => null!;
    public override string ToString() => "null";
}