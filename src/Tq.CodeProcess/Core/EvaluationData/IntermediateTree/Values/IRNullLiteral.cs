using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IRNullLiteral(SyntaxNode origin): IrExpression(origin)
{
    public override ITypeReference Type => null!;
    public override string ToString() => "null";
}