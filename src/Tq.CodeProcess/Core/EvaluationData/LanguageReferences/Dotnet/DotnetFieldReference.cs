using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

public class DotnetFieldReference(DotnetFieldObject reference) : Reference
{
    public readonly DotnetFieldObject Reference = reference;
    public override ITypeReference Type => (Reference.FieldType as ITypeReference)!;

    public override string ToString() => $"Fld<{Reference.Name}>";
}
