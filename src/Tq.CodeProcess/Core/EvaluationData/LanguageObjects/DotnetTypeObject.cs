using System.Text;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using AsmResolver.DotNet;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class DotnetTypeObject(TypeDefinition definition) : ContainerObject(null!, definition.Name!),
    IDotnetTypeContainer,
    IDotnetFieldContainer,
    IDotnetMethodContainer,
    IDotnetCtorDtorContainer
{
    public readonly TypeDefinition Reference = definition;
    public DotnetTypeObject? ParentType = null;

    public List<DotnetTypeObject> Types { get; } = [];
    public List<DotnetFieldObject> Fields { get; } = [];
    public List<DotnetMethodGroupObject> Methods { get; } = [];
    public List<DotnetMethodObject> Constructors { get; } = [];
    public DotnetTypeObject? Destructor { get; set; }

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

    public override LangObject? SearchChild(string name, SearchChildMode mode) => mode switch
    {
        SearchChildMode.All or SearchChildMode.OnlyStatic =>
            (LangObject?)Types.FirstOrDefault(e => e.Name == name)
            ?? Fields.FirstOrDefault(e => !e.IsStatic && e.Name == name)
            ?? Methods.FirstOrDefault(e => e.Name == name)
            ?? ParentType?.SearchChild(name, mode),
        
        SearchChildMode.OnlyInstance =>
            Fields.FirstOrDefault(e => e.IsStatic && e.Name == name)
            ?? Methods.FirstOrDefault(e => e.Name == name)
            ?? ParentType?.SearchChild(name, mode),

        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };
}
