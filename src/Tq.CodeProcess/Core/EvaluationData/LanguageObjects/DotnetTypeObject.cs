using System.Text;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using AsmResolver.DotNet;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class DotnetTypeObject(ITypeDescriptor typeRef, TypeDefinition typeDef, string n) : ContainerObject(null!, n),
    IDotnetTypeContainer,
    IDotnetMethodContainer,
    IDotnetCtorDtorContainer
{
    public readonly ITypeDescriptor TypeDescriptor = typeRef;
    public readonly TypeDefinition TypeDefinition = typeDef;

    
    public List<DotnetTypeObject> Types { get; } = [];
    public List<DotnetMethodGroupObject> Methods { get; } = [];
    public List<DotnetMethodObject> Constructors { get; } = [];
    public DotnetTypeObject? Destructor { get; set; }
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"class {TypeDefinition.Name!.Value}");
        if (TypeDefinition.GenericParameters.Count > 0)
            sb.Append($" ({string.Join(", ", TypeDefinition.GenericParameters)})");
        sb.Append($" extends {TypeDefinition.BaseType?.Name!.Value ?? "Object"}");

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
}
