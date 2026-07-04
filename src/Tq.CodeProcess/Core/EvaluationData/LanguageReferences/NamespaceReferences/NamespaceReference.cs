using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.NamespaceReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;

public class NamespaceReference(BaseNamespaceObject nmsp) : Reference
{
    public readonly BaseNamespaceObject NamespaceObject = nmsp;
    public override ITypeReference Type => new SolvedNamespaceReference(nmsp);
}
