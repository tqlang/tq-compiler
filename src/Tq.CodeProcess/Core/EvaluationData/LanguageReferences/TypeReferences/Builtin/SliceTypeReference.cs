namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class SliceTypeReference(TypeReference elementType) : TypeReference
{
    public TypeReference ElementType { get; set; } = elementType;
    
    public override Alignment Length => new (0, 2);
    public override Alignment Alignment => new (0, 1);

    public override bool IsGeneric => ElementType.IsGeneric;

    public override string ToString() => $"[]{ElementType}";
}
