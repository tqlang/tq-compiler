using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences;

public class NamespaceReference(NamespaceObject nmsp) : LanguageReference
{
    public readonly NamespaceObject NamespaceObject = nmsp;
    public override TypeReference Type => null!;
}
