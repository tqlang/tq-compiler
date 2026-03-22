using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.NamespaceReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;

public class NamespaceReference(BaseNamespaceObject nmsp) : LanguageReference
{
    public readonly BaseNamespaceObject NamespaceObject = nmsp;
    public override TypeReference Type => new SolvedNamespaceTypeReference(nmsp);
}
