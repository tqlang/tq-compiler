using AsmResolver.DotNet;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class DotnetStaticClassObject(string fullNamespace, TypeDefinition type) : BaseNamespaceObject(fullNamespace)
{
    public readonly string FullNamespace = fullNamespace;
    public readonly TypeDefinition Type = type;

    public readonly List<DotnetMethodGroupObject> Methods = [];
    public readonly List<DotnetFieldObject> Fields = [];
    public readonly List<DotnetTypeObject> Types = [];
    
    public override string ToString() => $"(.NET) Namespace '{FullNamespace}' => static class {Type.Name}";
    public override string ToSignature() => $"Namespace {string.Join('.', Global)}";

    public override LangObject? SearchChild(string name, SearchChildMode mode)
    {
        var module = (DotnetModuleObject)Module!;
        
        var cacheMethod = Methods.FirstOrDefault(e => e.Name == name);
        if (cacheMethod != null) return cacheMethod;
        var methods = Type.Methods.Where(e => e.Name == name).ToArray();
        if (methods.Length > 0)
        {
            var methodGroup = new DotnetMethodGroupObject(methods[0].Name!);
            foreach (var i in methods)
                methodGroup.Overloads.Add(DotnetMembers.GetOrCreateFunctionObject(i, module));
            return methodGroup;
        }

        var cacheField = Fields.FirstOrDefault(e => e.Name == name);
        if (cacheField != null) return cacheField;
        var field = Type.Fields.FirstOrDefault(e => e.Name == name);
        if (field != null)
        {
            return DotnetMembers.GetOrCreateFieldObject(field, module);
        }
        
        var cacheType = Types.FirstOrDefault(e => e.Name == name);
        if (cacheType != null) return cacheType;
        var type = Type.NestedTypes.FirstOrDefault(e => e.Name == name);
        if (type != null)
        {
            return DotnetMembers.GetOrCreateTypeObject(type, module);
        }

        return null;
    }
}
