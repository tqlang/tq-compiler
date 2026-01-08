using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.CodeObjects;

public class ParameterObject(TypeReference type, string name)
{
    public readonly string Name = name;
    public TypeReference Type { get; set; } = type;

    public int index = 0;

    public override bool Equals(object? obj)
    {
        if (obj is not ParameterObject parameter) return false;
        return parameter.Name == Name && parameter.Type == Type &&  parameter.index == index;
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        return hash.ToHashCode();
    }
    public override string ToString() => $"$({index:D2}) Parameter '{Name}': {Type?.ToString() ?? "<!nil>"}";
}
