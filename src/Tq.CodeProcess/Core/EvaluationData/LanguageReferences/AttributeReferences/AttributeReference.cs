using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;

public abstract class AttributeReference(AttributeNode node)
{
    public readonly AttributeNode syntaxNode = node;

}
