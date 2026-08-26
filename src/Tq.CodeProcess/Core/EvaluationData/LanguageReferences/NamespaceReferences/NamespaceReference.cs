using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.NamespaceReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;

public class NamespaceReference(BaseNamespaceObject nmsp) : Reference
{
    public readonly BaseNamespaceObject NamespaceObject = nmsp;
    public override ITypeReference Type => new SolvedNamespaceReference(nmsp);
}
