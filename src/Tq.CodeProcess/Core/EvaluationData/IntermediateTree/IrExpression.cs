using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;

public abstract class IrExpression(SyntaxNode origin) : IrNode(origin)
{
    public abstract TypeReference Type { get; }
}
