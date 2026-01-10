namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;

public class RuntimeIntegerTypeReference : IntegerTypeReference
{
    public readonly bool Signed;
    public readonly Alignment BitSize;

    public override Alignment Length => BitSize;
    public override Alignment Alignment => BitSize;
    
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
