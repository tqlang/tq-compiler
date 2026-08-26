namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public partial class StructureDeclarationNode : ControlNode
{
    public IdentifierNode Identifier => (IdentifierNode)_children[1];
    public bool HasGenericArguments => _children[2] is ParameterCollectionNode;
    public bool HasExtendsImplements => _children[HasGenericArguments ? 3 : 2] is ExtendsImplementsNode;
    
    public ParameterCollectionNode? ParameterCollection
    => HasGenericArguments ? (ParameterCollectionNode)_children[2] : null;
    public ExtendsImplementsNode? ExtendsImplements
    => HasExtendsImplements ? (ExtendsImplementsNode)_children[HasGenericArguments ? 3 : 2] : null;
    public BlockNode Body
    => (BlockNode)_children[
        HasGenericArguments
        ? (HasExtendsImplements ? 4 : 3)
        : (HasExtendsImplements ? 3 : 2)
    ];
}
