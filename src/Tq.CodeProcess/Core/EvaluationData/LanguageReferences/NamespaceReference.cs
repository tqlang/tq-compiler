using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;

public class NamespaceReference(NamespaceObject nmsp) : LanguageReference
{
    public readonly NamespaceObject NamespaceObject = nmsp;
    public override TypeReference Type => null!;
}
