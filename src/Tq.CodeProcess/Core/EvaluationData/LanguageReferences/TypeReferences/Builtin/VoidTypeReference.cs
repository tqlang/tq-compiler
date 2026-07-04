namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class VoidTypeReference: BuiltInTypeReference
{
    public override bool IsGeneric => false;
    public override ITypeReference Type => new TypeTypeReference(this);
    public override string ToString() => "void";
}
