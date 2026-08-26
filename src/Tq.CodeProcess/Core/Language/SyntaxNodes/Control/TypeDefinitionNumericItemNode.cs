namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class TypeDefinitionNumericItemNode : TypeDefinitionItemNode
{
 
    public IntegerLiteralNode Key => (IntegerLiteralNode)_children[0];
    
}
