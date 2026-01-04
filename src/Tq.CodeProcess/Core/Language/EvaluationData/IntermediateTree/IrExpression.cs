using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree;

public abstract class IrExpression(SyntaxNode origin) : IRNode(origin)
{
    public abstract TypeReference Type { get; }
}
