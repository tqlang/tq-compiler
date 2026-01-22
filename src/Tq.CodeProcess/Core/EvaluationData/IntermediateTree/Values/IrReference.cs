using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public abstract class IrReference(SyntaxNode origin) : IrExpression(origin)
{
}
