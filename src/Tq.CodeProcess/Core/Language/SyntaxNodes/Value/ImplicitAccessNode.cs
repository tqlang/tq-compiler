using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

public class ImplicitAccessNode() : ValueNode(default)
{
    public override string ToString() => $"?";
}
