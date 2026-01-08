using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;

public abstract class TypeReference: LanguageReference
{
    public override TypeReference Type => this;
    public abstract Alignment Length { get; }
    public abstract Alignment Alignment { get; }
    public bool ReturnsValue => this is not (VoidTypeReference or NoReturnTypeReference);
}
