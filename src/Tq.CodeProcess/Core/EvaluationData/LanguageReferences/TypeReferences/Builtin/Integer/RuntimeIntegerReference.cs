using Abstract.CodeProcess.Core;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;

public class RuntimeIntegerTypeReference : IntegerTypeReference
{
    public readonly bool Signed;
    public readonly Alignment BitSize;
    
    public override bool IsGeneric => false;
    public override ITypeReference Type => new TypeTypeReference(this);
    
    public RuntimeIntegerTypeReference(bool signed, byte size)
    {
        Signed = signed;
        BitSize = new Alignment(size, 0);
    }
    public RuntimeIntegerTypeReference(bool signed)
    {
        Signed = signed;
        BitSize = new Alignment(0, 1);
    }
    
    public override string ToString() => (Signed ? 'i' : 'u') + $"{BitSize.Bits}";
}
