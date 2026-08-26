using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

public class DotnetGenericTypeParamReference(int index) : Reference, ITypeReference
{
    public readonly int Index = index;
    public override ITypeReference Type => new TypeTypeReference(this);
    public bool IsGeneric => true;
    
    public override string ToString() => $"!{Index}";
}
