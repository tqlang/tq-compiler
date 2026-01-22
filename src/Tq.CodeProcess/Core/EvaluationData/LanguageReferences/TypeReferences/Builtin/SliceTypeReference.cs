namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class SliceTypeReference(TypeReference internalType) : TypeReference
{
    public TypeReference InternalType { get; set; } = internalType;
    
    public override Alignment Length => new (0, 2);
    public override Alignment Alignment => new (0, 1);

    public override bool IsGeneric => InternalType.IsGeneric;

    public override string ToString() => $"[]{InternalType}";
}
