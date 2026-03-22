using System.Text;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Imports;

public class SpecificImportObject(FromImportNode node, string[] namespacePath) : ImportObject
{
    public readonly FromImportNode Node = node;
    public readonly string[] NamespacePath = namespacePath;
    public ContainerObject Container = null!;
    public Dictionary<string, (string path, LangObject obj)> Imports { get; } = [];

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (Container != null!) sb.Append($"from {string.Join('.', Container.Global)} import {{ ");
        else sb.Append($"from {string.Join('.', NamespacePath)} import {{ ");

        foreach (var i in Imports)
        {
            if (i.Value.obj != null! && i.Key == i.Value.obj.Name) sb.Append(i.Value.obj.Name);
            else if (i.Value.obj == null! && i.Key == i.Value.path) sb.Append(i.Value.path);
            else if (i.Value.obj != null) sb.Append($"{i.Value.obj.Name} as {i.Key}");
            else sb.Append($"{i.Value.path} as {i.Key}");
            sb.Append(',');
        }

        sb.Length--;
        
        sb.Append(" }");
        return sb.ToString();
    }

    public override LangObject? SearchReference(string reference)
        => Imports.TryGetValue(reference, out var v) ? v.obj : null;
}
