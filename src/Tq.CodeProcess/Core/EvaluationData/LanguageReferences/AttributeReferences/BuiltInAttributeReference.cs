using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.AttributeReferences;

public class BuiltInAttributeReference(AttributeNode node, BuiltinAttributes bia) : AttributeReference(node)
{
    public readonly BuiltinAttributes Attribute = bia;
}
