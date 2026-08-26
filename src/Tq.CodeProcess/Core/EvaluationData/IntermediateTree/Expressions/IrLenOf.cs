using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrLenOf(SyntaxNode origin, IrExpression ofvalue) : IrExpression(origin)
{
    public override ITypeReference Type => new RuntimeIntegerTypeReference(false);
    public readonly IrExpression OfValue = ofvalue;

    public override string ToString() => $"lenof({OfValue})";
}