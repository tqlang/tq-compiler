namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;

public class RuntimeIntegerTypeReference : IntegerTypeReference
{
    public readonly bool Signed;
    public readonly bool PtrSized;
    public readonly byte BitSize = 0;

    public override Alignment Length => PtrSized ? new Alignment(0, 1) : new Alignment(BitSize, 0);
    public override Alignment Alignment => PtrSized ? new Alignment(0, 1) : new Alignment(BitSize, 0);
    
    public RuntimeIntegerTypeReference(bool signed, byte size)
    {
        Signed = signed;
        PtrSized = false;
        BitSize = size;
    }
    public RuntimeIntegerTypeReference(bool signed)
    {
        Signed = signed;
        PtrSized = true;
    }

    
    public override string ToString() => (Signed ? 'i' : 'u') + (PtrSized ? "ptr" : $"{BitSize}");
}
