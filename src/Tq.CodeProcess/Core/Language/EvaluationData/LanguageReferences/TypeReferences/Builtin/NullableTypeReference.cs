namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class NullableTypeReference(TypeReference internaltype) : BuiltInTypeReference
{
    public TypeReference InternalType { get; set; } = internaltype;
    public override Alignment Length => new Alignment(0, 1) + InternalType.Length;
    public override Alignment Alignment => InternalType.Alignment;
    public override string ToString() => $"?{InternalType}";
}