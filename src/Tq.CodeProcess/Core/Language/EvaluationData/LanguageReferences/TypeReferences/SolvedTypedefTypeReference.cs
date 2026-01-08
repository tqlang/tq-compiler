using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;

public class SolvedTypedefTypeReference(TypedefObject typedef) : TypeReference
{
    public readonly TypedefObject Typedef = typedef;
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;
    public override string ToString() => $"Typedef<{string.Join('.', Typedef.Global)}>";
}