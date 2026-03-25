using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrRef(SyntaxNode origin, IrExpression v) : IrExpression(origin)
{
    public IrExpression Expression = v;
    public override string ToString() => $"&{Expression}";
    public override TypeReference Type => new ReferenceTypeReference(Expression.Type);
}
