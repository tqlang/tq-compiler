using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

public class DotnetMethodGroupReference(DotnetMethodGroupObject r) : Reference
{
    public readonly DotnetMethodGroupObject MethodGroup = r;
    public override ITypeReference Type => null!;
    public override string ToString() => $"MGr<{MethodGroup.Name}>";
}
