using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.Misc;

public struct GenericOverloadResult(ICallable c, TypeReference[] types) : ISolvedOverloadResult
{
    public readonly ICallable Callable = c;
    public readonly TypeReference[] Types = types;
}
