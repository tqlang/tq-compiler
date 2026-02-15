using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;

public class DotnetGenericMethodParamReference(int index) : TypeReference
{
    public readonly int Index = index;
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;

    public override string ToString() => $"!!{Index}";
}