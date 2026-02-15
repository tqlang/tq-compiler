using System.Text;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class ModuleObject(string n) : ContainerObject(null!, n),
    INamespaceContainer
{
    public bool ReferenceOnly = false;
    public List<BaseNamespaceObject> Namespaces { get; } = [];

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (ReferenceOnly) sb.AppendLine($"module {Name} {{");
        else sb.AppendLine($"module {Name} refonly {{");
        
        foreach (var i in Namespaces) sb.AppendLine(i.ToString().TabAll());
        
        sb.AppendLine("}");
        return sb.ToString();
    }
    public override string ToSignature() => $"module {Name}";
}
