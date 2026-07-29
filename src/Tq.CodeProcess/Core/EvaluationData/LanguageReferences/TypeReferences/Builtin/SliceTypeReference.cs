namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class SliceTypeReference(Reference elementType) : BuiltInTypeReference
{
    public Reference ElementType { get; set; } = elementType;
    
    public override bool IsGeneric => ElementType is ITypeReference { IsGeneric: true };
    public override bool IsSolved => ElementType.IsSolved;
    public override ITypeReference Type => new TypeTypeReference(this);

    public override string ToString() => $"[]{ElementType}";
}
