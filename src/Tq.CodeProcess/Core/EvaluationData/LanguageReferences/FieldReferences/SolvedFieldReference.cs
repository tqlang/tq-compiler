using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.FieldReferences;

public class SolvedFieldReference(FieldObject field) : FieldReference
{
    public readonly FieldObject Field = field;
    public override ITypeReference Type => (ITypeReference)Field.Type;

    public override string ToString() => $"Field<{string.Join('.', Field.Global)}>";
}
