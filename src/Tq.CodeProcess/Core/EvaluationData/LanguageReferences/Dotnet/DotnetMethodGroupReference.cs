using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;

public class DotnetMethodGroupReference(DotnetMethodGroupObject r) : LanguageReference
{
    public readonly DotnetMethodGroupObject MethodGroup = r;
    public override TypeReference Type => null!;
    public override string ToString() => $"MGr<{MethodGroup.Name}>";
}
