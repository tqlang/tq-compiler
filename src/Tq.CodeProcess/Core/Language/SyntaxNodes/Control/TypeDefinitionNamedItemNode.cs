using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

public class TypeDefinitionNamedItemNode : TypeDefinitionItemNode
{ 
    public IdentifierNode Key => (IdentifierNode)_children[0];
    public ExpressionNode? Value => _children.Count == 3 ? (ExpressionNode)_children[2] : null;
}
