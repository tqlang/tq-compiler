using System.Text;
using AsmResolver.DotNet;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class DotnetModuleObject(string n) : BaseModuleObject(n)
{
    private List<ModuleDefinition> _manifestModules = [];
    public ModuleDefinition[] ManifestModules => [.._manifestModules];
    public Dictionary<string, BaseNamespaceObject> Namespaces { get; } = [];
    public Dictionary<string, DotnetTypeObject> Types { get; } = [];

    public void AddModule(ModuleDefinition moduleDefinition) => _manifestModules.Add(moduleDefinition);
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"module {Name} refonly {{");
        sb.AppendLine("}");
        return sb.ToString();
    }
    public override LangObject? SearchChild(string name, SearchChildMode mode) => null;
}
