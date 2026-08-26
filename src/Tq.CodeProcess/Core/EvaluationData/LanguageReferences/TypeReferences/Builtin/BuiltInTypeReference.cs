using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public abstract class BuiltInTypeReference : Reference, ITypeReference
{
    public virtual bool IsGeneric { get; }
}
