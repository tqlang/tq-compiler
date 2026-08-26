using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.CodeReferences;

public class SelfReference : Reference
{
    public override ITypeReference Type => null!;
    public override string ToString() => "self";
}
