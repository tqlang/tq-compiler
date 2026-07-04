namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class IgnoreTypeReference : BuiltInTypeReference
{
    public bool IsGeneric => false;
    public override ITypeReference Type => new TypeTypeReference(this);

    public override string ToString() => "%ignored%";
}
