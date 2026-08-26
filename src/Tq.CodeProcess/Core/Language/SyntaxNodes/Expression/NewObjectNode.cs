namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class NewObjectNode: ExpressionNode
{
    public ExpressionNode Type => (ExpressionNode)Children[1];
    public ExpressionNode[] Arguments => ((ArgumentCollectionNode)Children[2]).Arguments;
    public BlockNode? Inlined => Children.Length == 4 ? (BlockNode)Children[3] : null;

    public override string ToString() => $"new {Type}({string.Join(", ", Arguments)})";
}