using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.NamespaceReferences;

public class SolvedNamespaceTypeReference(BaseNamespaceObject nmsp) : TypeReference
{
    public readonly BaseNamespaceObject Namespace = nmsp;
    public override TypeReference Type => new TypeTypeReference(new SolvedNamespaceTypeReference(Namespace));
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;


    public override string ToString() => $"{Namespace}";
}