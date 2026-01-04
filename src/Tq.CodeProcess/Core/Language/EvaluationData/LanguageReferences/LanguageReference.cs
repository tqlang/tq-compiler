using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences;

public abstract class LanguageReference
{
    public abstract TypeReference Type { get; }
}
