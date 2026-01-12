using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;

public class IrLenOf(SyntaxNode origin, IrExpression ofvalue) : IrExpression(origin)
{
    public override TypeReference Type => new RuntimeIntegerTypeReference(false);
    public readonly IrExpression OfValue = ofvalue;

    public override string ToString() => $"lenof {OfValue}";
}