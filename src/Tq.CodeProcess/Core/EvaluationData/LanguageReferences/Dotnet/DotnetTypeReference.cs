using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;

public class DotnetTypeReference(DotnetTypeObject dotnetTypeObject) : TypeReference
{
    public readonly DotnetTypeObject Reference = dotnetTypeObject;

    public override string ToString() => Reference.Name;
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;
}