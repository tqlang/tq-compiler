

using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

public class GenericTypeReference(ParameterObject param) : Reference, ITypeReference
{
    public readonly ParameterObject Parameter = param;
    
    public Alignment Length => 0;
    public Alignment Alignment => 0;

    public bool IsGeneric => true;
    public override ITypeReference Type => new TypeTypeReference(this);

    public override string ToString() => Parameter.Name;
}
