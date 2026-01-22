namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;

public class ComptimeIntegerTypeReference : IntegerTypeReference
{
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;
    public override string ToString() => "comptime_int";
}
