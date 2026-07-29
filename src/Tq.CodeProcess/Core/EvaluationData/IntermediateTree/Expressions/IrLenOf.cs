using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrLenOf(SyntaxNode origin, IrExpression ofvalue) : IrExpression(origin)
{
    public override ITypeReference Type => new RuntimeIntegerTypeReference(false);
    public readonly IrExpression OfValue = ofvalue;

    public override string ToString() => $"lenof({OfValue})";
}