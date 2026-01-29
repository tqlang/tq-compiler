using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.Misc;

public struct DotnetOverloadResult(ICallable c, TypeReference[] g, TypeReference[] p) : ISolvedOverloadResult
{
    public readonly ICallable Callable = c;
    public readonly TypeReference[] Generics = g;
    public readonly TypeReference[] Parameters = p;
}
