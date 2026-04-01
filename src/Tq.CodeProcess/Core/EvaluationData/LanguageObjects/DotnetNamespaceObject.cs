using System.Text;
using AsmResolver.DotNet;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class DotnetNamespaceObject(string fullNamespace) : BaseNamespaceObject(fullNamespace)
{
    public readonly string FullNamespace = fullNamespace;
    public List<ITypeDefOrRef> DotnetTypes = [];
    public readonly List<DotnetTypeObject> Types = [];
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"(.NET) Namespace '{FullNamespace}' {{");

        
        sb.AppendLine("}");
        return sb.ToString();
    }
    public override string ToSignature() => $"Namespace {string.Join('.', Global)}";

    public override LangObject? SearchChild(string name, SearchChildMode mode)
    {
        var module = (DotnetModuleObject)Module!;

        var cacheType = Types.FirstOrDefault(e => e.Name == name);
        if (cacheType != null) return cacheType;
        
        var type = DotnetTypes.FirstOrDefault(e => DotnetMembers.NormalizeTypeName(e.Name!) == name);
        return type != null ? DotnetMembers.GetOrCreateTypeObject(type, module) : null;
    }
}
