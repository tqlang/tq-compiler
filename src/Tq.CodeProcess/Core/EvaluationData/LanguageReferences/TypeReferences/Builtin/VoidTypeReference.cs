namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class VoidTypeReference: TypeReference
{
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;
    public override string ToString() => "void";
}
