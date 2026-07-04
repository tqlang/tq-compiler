using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

public interface ITypeReference
{
    public bool ReturnsValue => this is not (VoidTypeReference or NoReturnTypeReference);
    public bool IsGeneric { get; }
}
