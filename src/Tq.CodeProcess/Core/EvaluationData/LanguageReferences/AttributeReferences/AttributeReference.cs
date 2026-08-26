using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;

public abstract class AttributeReference(AttributeNode node)
{
    public readonly AttributeNode syntaxNode = node;

}
