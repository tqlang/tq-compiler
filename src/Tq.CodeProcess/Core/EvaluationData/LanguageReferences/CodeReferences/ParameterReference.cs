using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.CodeReferences;

public class ParameterReference(ParameterObject param) : LanguageReference
{
    public readonly ParameterObject Parameter = param;
    public override TypeReference Type => Parameter.Type;

    public override string ToString() => $"arg.{Parameter.Index:D2}";
}
