using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Misc;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

public class FunctionDeclarationNode : ControlNode
{
    public IdentifierNode Identifier => (IdentifierNode)_children[1];
    public ParameterCollectionNode ParameterCollection => (ParameterCollectionNode)_children[2];
    public ExpressionNode? ReturnType => _children.Count >= 4 ? _children[3] as ExpressionNode : null;
    
    public BlockNode? GetFunctionBody()
    {
        // Function body options ([..] means constant):
        //  [func <ident> <params>] <type> <body>      (len 5, type: 3, body: 4)
        //  [func <ident> <params>] <type>             (len 4, type: 3, body:  )
        //  [func <ident> <params>] <body>             (len 4, type:  , body: 3)
        //  [func <ident> <params>]                    (len 3, type:  , body:  )
        
        var funContent = _children;
        var funLen = funContent.Count;

        if (funLen >= 4) return funContent[funLen-1] as BlockNode;
        return null;

    }
}
