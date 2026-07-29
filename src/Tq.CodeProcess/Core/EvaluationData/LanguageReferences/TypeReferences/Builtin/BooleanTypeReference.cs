namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class BooleanTypeReference : BuiltInTypeReference
{
    public override bool IsGeneric => false;
    public override ITypeReference Type => new TypeTypeReference(this);

    public override string ToString() => "bool";
}