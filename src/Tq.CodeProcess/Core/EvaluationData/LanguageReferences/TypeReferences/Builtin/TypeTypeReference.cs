namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class TypeTypeReference(TypeReference? referenced) : TypeReference
{
    public readonly TypeReference? ReferencedType = referenced;
    
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;
    
    public override string ToString() => ReferencedType != null ? $"type({ReferencedType})" : $"type";
}
