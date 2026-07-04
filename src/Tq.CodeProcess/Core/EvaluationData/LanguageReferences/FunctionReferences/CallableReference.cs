using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;

public class CallableReference(ICallable callable) : FunctionReference
{
    public readonly ICallable Callable = callable;
    public override ITypeReference Type => new FunctionTypeReference(
        Callable.ReturnType!,
        Callable.Parameters.Select(e => e.Type).ToArray());

    public override string ToString() => $"Callable<{string.Join('.', ((LangObject)Callable).Global)}>";
}