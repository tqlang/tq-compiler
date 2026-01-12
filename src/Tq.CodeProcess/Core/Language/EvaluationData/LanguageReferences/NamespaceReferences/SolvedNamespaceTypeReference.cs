using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.NamespaceReferences;

public class SolvedNamespaceTypeReference(NamespaceObject nmsp) : TypeReference
{
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;

    public readonly NamespaceObject Namespace = nmsp;

    public override string ToString() => $"{Namespace}";
}