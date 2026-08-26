namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class DestructorDeclarationNode : ControlNode
{
    public ParameterCollectionNode ParameterCollection => (ParameterCollectionNode)_children[1];
    public BlockNode? Body => _children.Count == 3 ? (BlockNode)_children[2] : null;
}
