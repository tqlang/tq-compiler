using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypedefReferences;

public class SolvedTypedefNamedValueReference(TypedefNamedValue namedValue) : Reference
{
    public override ITypeReference Type => new TypedefReference((TypedefObject)NamedValue.Parent);
    public readonly TypedefNamedValue NamedValue = namedValue;
    public override string ToString() => $"{NamedValue:sig}";
}
