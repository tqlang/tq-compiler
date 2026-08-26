namespace Tq.CodeProcess.Core.Language.SyntaxNodes;

public class ParameterCollectionNode : SyntaxNode
{
    public TypedIdentifierNode[] Items => [.. _children[1..^1].Select(e => (TypedIdentifierNode)e)];
    public override string ToString() => $"({string.Join(", ", _children[1..^1])})";
}
