using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;

public class UnsolvedAttributeReference(AttributeNode node) : AttributeReference(node)
{
}
