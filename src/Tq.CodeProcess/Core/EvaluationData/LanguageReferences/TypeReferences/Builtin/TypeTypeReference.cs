namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class TypeTypeReference(ITypeReference? referenced) : BuiltInTypeReference
{
    public readonly ITypeReference? ReferencedType = referenced;
    
    public override bool IsGeneric => false;
    public override ITypeReference Type => new TypeTypeReference(this);
    
    public override string ToString() => ReferencedType != null ? $"type({ReferencedType})" : $"type";
}
