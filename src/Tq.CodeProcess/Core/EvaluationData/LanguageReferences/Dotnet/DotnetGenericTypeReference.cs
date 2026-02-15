using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;

public class DotnetGenericTypeReference(DotnetTypeObject baseRef, TypeReference[] args) : TypeReference
{
    public readonly DotnetTypeObject Reference = baseRef;
    public readonly TypeReference[] GenericArguments = args;
    
    public override string ToString() => Reference.Name;
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;
}
