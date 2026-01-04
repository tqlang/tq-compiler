using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.CodeReferences;

public class ParameterReference(ParameterObject param) : LanguageReference
{
    public readonly ParameterObject Parameter = param;
    public override TypeReference Type => Parameter.Type;

    public override string ToString() => $"arg.{Parameter.index:D2}";
}
