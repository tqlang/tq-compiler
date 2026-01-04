using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FunctionReferences;

public class SolvedFunctionReference(FunctionObject fun) : FunctionReference
{
    public readonly FunctionObject Function = fun;
    public override TypeReference Type => new FunctionTypeReference(fun.ReturnType, fun.Parameters.Select(e => e.Type).ToArray());

    public override string ToString() => $"Fun<{string.Join('.', Function.Global)}>";
}