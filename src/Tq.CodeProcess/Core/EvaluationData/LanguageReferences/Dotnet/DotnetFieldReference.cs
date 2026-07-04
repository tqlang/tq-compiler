using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;

public class DotnetFieldReference(DotnetFieldObject reference) : Reference
{
    public readonly DotnetFieldObject Reference = reference;
    public override ITypeReference Type => (Reference.FieldType as ITypeReference)!;

    public override string ToString() => $"Fld<{Reference.Name}>";
}
