using System.Text;
using AsmResolver.DotNet;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class DotnetTypeObject(string name, TypeDefinition definition) : ContainerObject(null!, name)
{
    public readonly TypeDefinition Reference = definition;
    public DotnetTypeObject? ParentType = null;

    public List<DotnetTypeObject> Types = [];
    public List<DotnetFieldObject> Fields = [];
    public List<DotnetMethodGroupObject> Methods = [];
    public List<DotnetMethodObject> Constructors = [];
    public DotnetTypeObject? Destructor = null;

    public bool IsStatic => Reference is { IsAbstract: true, IsSealed: true };
    public bool IsValueType => Reference.IsValueType;
    public bool IsEnum => Reference.IsEnum;
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        if (IsStatic) sb.Append("static ");
        sb.Append(IsValueType ? "struct" : "class");
        sb.Append($" {Reference.Name!.Value}");
        if (Reference.GenericParameters.Count > 0) sb.Append($" <{string.Join(", ", Reference.GenericParameters)}>");
        if (ParentType != null) sb.Append($" extends {ParentType?.Reference.Name!.Value}");

        if (Types.Count + Methods.Count + Constructors.Count > 0 || Destructor != null)
        {
            sb.AppendLine(" {");
            foreach (var i in Types) sb.AppendLine(i.ToString().TabAll());
            foreach (var i in Constructors) sb.AppendLine(i.ToString().TabAll());
            if (Destructor != null) sb.AppendLine($" Destructor.ToString().TabAll()");
            foreach (var i in Methods) sb.AppendLine(i.ToString().TabAll());
            sb.Append('}');
        }
        else sb.Append(" {}");
        
        return sb.ToString();
    }
    public override string ToSignature() => Name;

    public override LangObject? SearchChild(string name, SearchChildMode mode)
    {
        var module = (DotnetModuleObject)Module!;
        
        var cacheMethod = Methods.FirstOrDefault(e => e.Name == name);
        if (cacheMethod != null) return cacheMethod;
        var methods = Reference.Methods.Where(e => e.Name == name).ToArray();
        if (methods.Length > 0)
        {
            var methodGroup = new DotnetMethodGroupObject(methods[0].Name!);
            foreach (var i in methods)
                methodGroup.Overloads.Add(DotnetMembers.GetOrCreateFunctionObject(i, module));
            return methodGroup;
        }

        var cacheField = Fields.FirstOrDefault(e => e.Name == name);
        if (cacheField != null) return cacheField;
        var field = Reference.Fields.FirstOrDefault(e => e.Name == name);
        if (field != null)
        {
            return DotnetMembers.GetOrCreateFieldObject(field, module);
        }
        
        var cacheType = Types.FirstOrDefault(e => e.Name == name);
        if (cacheType != null) return cacheType;
        var type = Reference.NestedTypes.FirstOrDefault(e => e.Name == name);
        if (type != null)
        {
            return DotnetMembers.GetOrCreateTypeObject(type, module);
        }

        if (ParentType == null) return null;
        return ParentType.SearchChild(name, mode);
    }
}
