namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class SliceTypeReference(TypeReference internaltype) : TypeReference
{
    public TypeReference InternalType { get; set; } = internaltype;
    public override Alignment Length => new (0, 2);
    public override Alignment Alignment => new (0, 1);
    public override string ToString() => $"[]{InternalType}";
}
