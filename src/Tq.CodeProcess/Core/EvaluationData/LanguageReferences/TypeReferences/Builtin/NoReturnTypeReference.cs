namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class NoReturnTypeReference: BuiltInTypeReference
{
    public override bool IsGeneric => false;
    public override ITypeReference Type => new TypeTypeReference(this);
    
    public override string ToString() => "noreturn";
}