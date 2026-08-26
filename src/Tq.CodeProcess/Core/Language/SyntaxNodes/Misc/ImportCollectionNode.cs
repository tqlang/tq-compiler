namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class ImportCollectionNode : SyntaxNode
{
    public IEnumerable<SyntaxNode> Content => _children.Count > 2
        ? _children[1..^1]
        : [];
}
