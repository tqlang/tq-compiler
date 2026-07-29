using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree;

public class IrNode(SyntaxNode origin)
{
    public readonly SyntaxNode Origin = origin;
}