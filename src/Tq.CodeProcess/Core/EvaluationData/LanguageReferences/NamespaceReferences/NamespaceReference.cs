using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.NamespaceReferences;

public class NamespaceReference(BaseNamespaceObject nmsp) : Reference
{
    public readonly BaseNamespaceObject NamespaceObject = nmsp;
    public override ITypeReference Type => new SolvedNamespaceReference(nmsp);
}
