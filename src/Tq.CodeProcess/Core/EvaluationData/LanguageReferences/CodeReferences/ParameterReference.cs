using Tq.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.CodeReferences;

public class ParameterReference(ParameterObject param) : Reference
{
    public readonly ParameterObject Parameter = param;
    public override ITypeReference Type => (ITypeReference)Parameter.Type;

    public override string ToString() => $"arg.{Parameter.Index:D2}";
}
