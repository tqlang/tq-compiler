using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;

public class DotnetMethodGroupReference(DotnetMethodGroupObject r) : Reference
{
    public readonly DotnetMethodGroupObject MethodGroup = r;
    public override ITypeReference Type => null!;
    public override string ToString() => $"MGr<{MethodGroup.Name}>";
}
