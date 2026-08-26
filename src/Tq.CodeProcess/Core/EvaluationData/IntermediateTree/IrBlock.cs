using System.Text;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;

public class IrBlock(SyntaxNode origin): IrNode(origin)
{
    public readonly List<IrNode> Content = [];

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var i in Content) sb.AppendLine(i.ToString());
        return sb.ToString().TrimEnd();
    }
}
