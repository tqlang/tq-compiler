using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;

public class LocalVariableObject(Reference? typeref, string name)//: LangObject(null!)
{
    public int index;
    public Reference? Type = typeref;
    public readonly string Name = name;
    
    public override string ToString() => $"$({index:D2}) Local '{Name}': {Type?.ToString() ?? "<!nil>"}";
}
