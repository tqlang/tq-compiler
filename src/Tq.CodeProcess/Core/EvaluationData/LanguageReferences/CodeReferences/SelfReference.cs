using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.CodeReferences;

public class SelfReference : Reference
{
    public override ITypeReference Type => null!;
    public override string ToString() => "self";
}
