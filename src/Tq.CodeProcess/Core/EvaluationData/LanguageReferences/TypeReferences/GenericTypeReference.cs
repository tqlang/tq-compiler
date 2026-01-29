

using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

public class GenericTypeReference(ParameterObject param) : TypeReference
{
    public readonly ParameterObject Parameter = param;
    
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;

    public override bool IsGeneric => true;

    public override string ToString() => Parameter.Name;
}
