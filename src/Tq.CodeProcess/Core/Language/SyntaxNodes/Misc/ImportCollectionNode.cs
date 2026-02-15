using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.SyntaxNodes.Misc;

public class ImportCollectionNode : SyntaxNode
{
    public IEnumerable<SyntaxNode> Content => _children.Count > 2
        ? _children[1..^1]
        : [];
}
