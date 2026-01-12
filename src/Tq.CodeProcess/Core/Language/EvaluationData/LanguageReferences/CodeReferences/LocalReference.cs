using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.CodeReferences;

public class LocalReference(LocalVariableObject local) : LanguageReference
{
    public readonly LocalVariableObject Local = local;
    public override TypeReference Type => Local.Type!;

    public override string ToString() => $"local.{Local.index:D2}";
}
