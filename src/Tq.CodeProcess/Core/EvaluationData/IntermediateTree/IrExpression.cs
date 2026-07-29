using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree;

public abstract class IrExpression(SyntaxNode origin) : IrNode(origin)
{
    public abstract ITypeReference Type { get; }
}
