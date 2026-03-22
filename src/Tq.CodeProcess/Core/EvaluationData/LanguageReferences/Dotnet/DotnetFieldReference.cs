using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;

public class DotnetFieldReference(DotnetFieldObject reference) : LanguageReference
{
    public readonly DotnetFieldObject Reference = reference;
    public override TypeReference Type => Reference.FieldType;

    public override string ToString() => $"Fld<{Reference.Name}>";
}
