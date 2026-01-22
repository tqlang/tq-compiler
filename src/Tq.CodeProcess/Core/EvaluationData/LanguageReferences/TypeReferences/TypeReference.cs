using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

public abstract class TypeReference: LanguageReference
{
    public override TypeReference Type => new TypeTypeReference(this);
    
    public abstract Alignment Length { get; }
    public abstract Alignment Alignment { get; }
    
    public virtual bool IsGeneric => false;
    
    public bool ReturnsValue => this is not (VoidTypeReference or NoReturnTypeReference);
}
