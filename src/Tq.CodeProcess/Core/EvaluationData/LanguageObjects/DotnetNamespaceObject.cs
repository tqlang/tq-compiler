using System.Text;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class DotnetNamespaceObject(string n) : BaseNamespaceObject(n),
    INamespaceContainer,
    IDotnetTypeContainer
{

    public List<DotnetTypeObject> Types { get; } = [];
    public List<BaseNamespaceObject> Namespaces { get; } = [];
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Namespace '{Name}' ('{string.Join('.', Global)}') {{");

        foreach (var i in Namespaces) sb.AppendLine($"{i}".TabAll());
        foreach (var i in Types) sb.AppendLine($"{i}".TabAll());

        sb.AppendLine("}");
        return sb.ToString();
    }
    public override string ToSignature() => $"Namespace {string.Join('.', Global)}";

    public override LangObject? SearchChild(string name)
        => (LangObject?)Types.FirstOrDefault(e => e.Name == name)
            ?? Namespaces.FirstOrDefault(e => e.Name == Name);
}
