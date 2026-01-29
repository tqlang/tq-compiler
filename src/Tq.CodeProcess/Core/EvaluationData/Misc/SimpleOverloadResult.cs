using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

namespace Abstract.CodeProcess.Core.EvaluationData.Misc;

public struct SimpleOverloadResult(ICallable c) : ISolvedOverloadResult
{
    public readonly ICallable Callable = c;
}
