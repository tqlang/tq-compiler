using System.Text;
using Abstract.CodeProcess.Core;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects.Attributes;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageObjects;

public class StructObject(SourceScript sourceScript, string n, StructureDeclarationNode synNode) : ContainerObject(sourceScript, n),
        IPublicModifier,
        IStaticModifier,
        IInternalModifier,
        IAbstractModifier,
        
        IFieldContainer,
        ICtorDtorContainer,
        IFunctionContainer
{
    public bool Public { get; set; } = false;
    public bool Static { get; set; } = false;
    public bool Internal { get; set; } = false;
    public bool Abstract { get; set; } = false;
    public bool Interface { get; set; } =  false;
    public bool Final { get; set; } =  false;
    public bool Generic => Parameters.Count == 0;
    
    
    public Reference? Extends { get; set; }
    public (FunctionObject parent, FunctionObject? overrided, bool isSealed)[]? VirtualTable { get; set; }
    public List<ParameterObject> Parameters { get; } = [];

    public List<FieldObject> Fields { get; } = [];
    public List<ConstructorObject> Constructors { get; } = [];
    public List<DestructorObject> Destructors { get; } = [];
    public List<FunctionGroupObject> Functions { get; } = [];
    
    public readonly StructureDeclarationNode SyntaxNode = synNode;

    public override LangObject? SearchChild(string name, SearchChildMode mode) => mode switch
    {
        SearchChildMode.All =>
            Fields.FirstOrDefault(e => e.Name == name)
            ?? (LangObject?)Functions.FirstOrDefault(e => e.Name == name),
            
        SearchChildMode.OnlyStatic =>
            Fields.FirstOrDefault(e => e.Name == name && e.Static)
            ?? (LangObject?)Functions.FirstOrDefault(e => e.Name == name),
        
        SearchChildMode.OnlyInstance =>
            Fields.FirstOrDefault(e => e.Name == name && !e.Static)
            ?? (LangObject?)Functions.FirstOrDefault(e => e.Name == name)
    };

    public override string ToString()
    {
        var sb = new StringBuilder();
        
        sb.Append(Public ? "public " : "private ");
        sb.Append(Static ? "static " : "instance ");
        if (Internal) sb.Append("internal ");
        sb.Append(Abstract ? "abstract " : "concrete ");

        sb.Append($"struct '{Name}'");
        if (Parameters.Count > 0) sb.Append($"({string.Join(", ", Parameters.Select(e => e.Name))})");
        if (Extends != null) sb.Append($" extends {Extends:sig}");
        sb.AppendLine(" {");
        
        foreach (var c in Fields) sb.AppendLine(c.ToString().TabAll());
        foreach (var c in Functions) sb.AppendLine(c.ToString().TabAll());
        foreach (var c in Constructors) sb.AppendLine(c.ToString().TabAll());
        foreach (var c in Destructors) sb.AppendLine(c.ToString().TabAll());

        sb.AppendLine("}");
        return sb.ToString();
    }
    public override string ToSignature() => $"{Name}";
}
