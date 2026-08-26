using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

namespace Abstract.CodeProcess.Core.EvaluationData.Misc;

public struct GenericOverloadResult(ICallable c, ITypeReference[] generics) : ISolvedOverloadResult
{
    public readonly ICallable Callable = c;
    public readonly ITypeReference[] GenericArgs = generics;
}
