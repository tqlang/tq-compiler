using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;

public class LocalVariableObject(Reference? typeref, string name)//: LangObject(null!)
{
    public int index;
    public Reference? Type = typeref;
    public readonly string Name = name;
    
    public override string ToString() => $"$({index:D2}) Local '{Name}': {Type?.ToString() ?? "<!nil>"}";
}
