using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;

public class UnsolvedAttributeReference(AttributeNode node) : AttributeReference(node)
{
}
