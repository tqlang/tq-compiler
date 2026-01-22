using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypedefReferences;

public class SolvedTypedefNamedValueReference(TypedefNamedValue namedValue) : TypeReference
{
    public override Alignment Length => Type.Length;
    public override Alignment Alignment => Type.Alignment;
    
    public readonly TypedefNamedValue NamedValue = namedValue;
    public override TypeReference Type => new SolvedTypedefTypeReference((TypedefObject)NamedValue.Parent);
    
    public override string ToString() => $"{NamedValue:sig}";
}
