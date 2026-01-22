using System.Text;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;

public class ParameterObject(TypeReference type, string name)
{
    public readonly string Name = name;
    public TypeReference Type { get; set; } = type;
    public int Index = 0;

    public bool IsGeneric => Type is TypeTypeReference;
    

    public override bool Equals(object? obj)
    {
        if (obj is not ParameterObject parameter) return false;
        return parameter.Name == Name && parameter.Type == Type &&  parameter.Index == Index;
    }
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Name);
        return hash.ToHashCode();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append($"$({Index:D2}) ");
        if (IsGeneric) sb.Append("generic ");
        sb.Append($"parameter '{Name}': ");
        sb.Append(Type?.ToString() ?? "<!nil>");
        
        return sb.ToString();
    }
}
