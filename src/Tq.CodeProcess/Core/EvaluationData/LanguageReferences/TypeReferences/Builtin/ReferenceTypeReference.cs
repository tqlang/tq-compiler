namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class ReferenceTypeReference(Reference internalType) : BuiltInTypeReference
{
    public Reference InternalType { get; set; } = internalType;
    
    public override bool IsGeneric => InternalType is ITypeReference { IsGeneric: true };
    public override bool IsSolved => InternalType.IsSolved;
    public override ITypeReference Type => new TypeTypeReference(this);

    public override string ToString() => $"*{InternalType}";
}
