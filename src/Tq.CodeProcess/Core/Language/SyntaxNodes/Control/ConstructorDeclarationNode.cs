using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Misc;

namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

public class ConstructorDeclarationNode : ControlNode
{
    public ParameterCollectionNode ParameterCollection => (ParameterCollectionNode)_children[1];
    public ExpressionNode? Returns => _children.Count > 2 ? _children[2] as ExpressionNode : null;
    public BlockNode? Body => GetBody();

    private BlockNode? GetBody()
    {
        // constructor (...) <type> {}
        // constructor (...) <type>
        // constructor (...) {}
        // constructor (...)

        return _children.Count switch
        {
            < 3 => null,
            3 => _children[2] as BlockNode,
            4 => (BlockNode)_children[3],
            _ => throw new ArgumentOutOfRangeException()
        };

    }
}
