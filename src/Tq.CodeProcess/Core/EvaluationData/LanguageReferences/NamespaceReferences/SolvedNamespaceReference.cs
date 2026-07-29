using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.NamespaceReferences;

public class SolvedNamespaceReference(BaseNamespaceObject nmsp) : Reference, ITypeReference
{
    public readonly BaseNamespaceObject Namespace = nmsp;
    public bool IsGeneric => false;
    public override ITypeReference Type => new TypeTypeReference(this);
    
    public override string ToString() => $"{Namespace}";
}