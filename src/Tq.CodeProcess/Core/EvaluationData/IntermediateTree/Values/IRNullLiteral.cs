using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IRNullLiteral(SyntaxNode origin): IrExpression(origin)
{
    public override ITypeReference Type => null!;
    public override string ToString() => "null";
}