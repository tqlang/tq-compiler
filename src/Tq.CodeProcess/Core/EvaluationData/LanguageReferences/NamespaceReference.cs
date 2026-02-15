using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;

public class NamespaceReference(TqNamespaceObject nmsp) : LanguageReference
{
    public readonly TqNamespaceObject TqNamespaceObject = nmsp;
    public override TypeReference Type => null!;
}
