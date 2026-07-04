using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.CodeReferences;

public class LocalReference(LocalVariableObject local) : Reference
{
    public readonly LocalVariableObject Local = local;
    public override ITypeReference Type => (Local.Type as ITypeReference)!;

    public override string ToString() => $"local.{Local.index:D2}";
}
