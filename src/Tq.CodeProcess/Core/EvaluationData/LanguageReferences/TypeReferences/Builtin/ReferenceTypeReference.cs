namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class ReferenceTypeReference(TypeReference internaltype) : TypeReference
{
    public TypeReference InternalType { get; set; } = internaltype;
    
    public override Alignment Length => new (0, 1);
    public override Alignment Alignment => new (0, 1);

    public override bool IsGeneric => InternalType.IsGeneric;

    public override string ToString() => $"*{InternalType}";
}
