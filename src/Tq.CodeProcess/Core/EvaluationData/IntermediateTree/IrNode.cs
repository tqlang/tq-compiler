using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;

public class IrNode(SyntaxNode origin)
{
    public readonly SyntaxNode Origin = origin;
}