using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypedefReferences;

public class SolvedTypedefNamedFieldReference(TypedefItemObject item) : TypeReference
{
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;
    
    public readonly TypedefItemObject Item = item;
    
    public override string ToString() => $"{string.Join('.', Item.Parent.Global)}.{Item.Name}";
}
