using System.Text;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class ModuleObject(string n) : LangObject(n),
    INamespaceContainer
{
    
    public List<NamespaceObject> Namespaces { get; } = [];

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"module {Name} {{");
        
        foreach (var i in Namespaces) sb.AppendLine(i.ToString().TabAll());
        
        sb.AppendLine("}");
        return sb.ToString();
    }
    public override string ToSignature() => $"module {Name}";
}
