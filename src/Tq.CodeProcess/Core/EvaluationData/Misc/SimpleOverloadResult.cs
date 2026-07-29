using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

namespace Tq.CodeProcess.Core.EvaluationData.Misc;

public struct SimpleOverloadResult(ICallable c) : ISolvedOverloadResult
{
    public readonly ICallable Callable = c;
}
