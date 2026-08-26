using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

public class DotnetTypeReference(DotnetTypeObject dotnetTypeObject) : Reference, ITypeReference
{
    public readonly DotnetTypeObject Reference = dotnetTypeObject;

    public override ITypeReference Type => new TypeTypeReference(this);
    public bool IsGeneric => false;

    public override string ToString() => Reference.Name;

}
