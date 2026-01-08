namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class BooleanTypeReference: TypeReference
{
    public override Alignment Length => new (1, 0);
    public override Alignment Alignment => new (1, 0);
    public override string ToString() => "bool";
}