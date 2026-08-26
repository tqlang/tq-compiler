using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;

public class BuiltInAttributeReference(AttributeNode node, BuiltinAttributes bia) : AttributeReference(node)
{
    public readonly BuiltinAttributes Attribute = bia;
}
