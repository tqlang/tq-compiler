using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;

public abstract class IrExpression(SyntaxNode origin) : IrNode(origin)
{
    public abstract ITypeReference Type { get; }
}
