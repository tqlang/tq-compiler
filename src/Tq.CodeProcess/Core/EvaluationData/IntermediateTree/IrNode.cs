using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;

public class IrNode(SyntaxNode origin)
{
    public readonly SyntaxNode Origin = origin;
}