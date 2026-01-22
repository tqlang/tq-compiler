namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class CharTypeReference(): BuiltInTypeReference
{
    public override Alignment Length => new (32, 0);
    public override Alignment Alignment => new (0, 1);
    public override string ToString() => "char";
}
