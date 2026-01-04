using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.CodeReferences;

public class SelfReference : LanguageReference
{
    public override TypeReference Type => null!;
    public override string ToString() => "self";
}
