using System.Text;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Imports;

public class SpecificImportObject(string[] namespacePath) : ImportObject
{
    public readonly string[] NamespacePath = namespacePath;
    public BaseNamespaceObject NamespaceObject = null!;
    public readonly Dictionary<string, (string path, LangObject obj)> Imports = [];

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (NamespaceObject != null!) sb.Append($"from {string.Join('.', NamespaceObject.Global)} import {{ ");
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
