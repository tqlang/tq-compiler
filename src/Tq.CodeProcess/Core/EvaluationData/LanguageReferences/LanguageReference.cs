using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;

public abstract class LanguageReference
{
    public abstract TypeReference Type { get; }
}
