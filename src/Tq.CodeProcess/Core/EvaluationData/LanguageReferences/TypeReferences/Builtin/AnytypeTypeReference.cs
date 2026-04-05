namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class AnytypeTypeReference : TypeReference
{
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;

    public override bool IsGeneric => false;

    public override string ToString() => "anytype";
}
