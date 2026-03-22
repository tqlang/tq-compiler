using System.Text;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class TqModuleObject(string n) : BaseModuleObject(n)
{
    public BaseNamespaceObject? Root { get; set; } = null!;
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"module {Name} {{");
        if (Root != null) sb.AppendLine(Root.ToString().TabAll());
        sb.AppendLine("}");
        return sb.ToString();
    }
    public override LangObject? SearchChild(string name, SearchChildMode mode) => Root?.SearchChild(name, mode);
}
