namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;

public class ComptimeIntegerTypeReference : IntegerTypeReference
{
    public override bool IsGeneric => false;
    public override ITypeReference Type => new TypeTypeReference(this);
    public override string ToString() => "comptime_int";
}
