using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;

public class DotnetTypeReference(DotnetTypeObject dotnetTypeObject) : Reference, ITypeReference
{
    public readonly DotnetTypeObject Reference = dotnetTypeObject;

    public override ITypeReference Type => new TypeTypeReference(this);
    public bool IsGeneric => false;

    public override string ToString() => Reference.Name;

}
