namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class CharTypeReference(): BuiltInTypeReference
{
    public bool IsGeneric => false;
    public override ITypeReference Type => new TypeTypeReference(this);
    
    public override string ToString() => "char";
}
