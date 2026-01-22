using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FieldReferences;

public class SolvedFieldReference(FieldObject field) : FieldReference
{
    public readonly FieldObject Field = field;
    public override TypeReference Type => Field.Type;

    public override string ToString() => $"Field<{string.Join('.', Field.Global)}>";
}
