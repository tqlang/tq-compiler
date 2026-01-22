using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;

public class SolvedCallableReference(ICallable callable) : FunctionReference
{
    public readonly ICallable Callable = callable;
    public override TypeReference Type => new FunctionTypeReference(
        Callable.ReturnType!,
        Callable.Parameters.Select(e => e.Type).ToArray());

    public override string ToString() => $"Callable<{string.Join('.', ((LangObject)Callable).Global)}>";
}