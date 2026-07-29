using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.Misc;

public struct GenericOverloadResult(ICallable c, ITypeReference[] generics) : ISolvedOverloadResult
{
    public readonly ICallable Callable = c;
    public readonly ITypeReference[] GenericArgs = generics;
}
