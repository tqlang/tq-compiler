using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.Attributes;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.Containers;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;

public class StructObject(string n, StructureDeclarationNode synNode) : ContainerObject(n),
        IPublicModifier,
        IStaticModifier,
        IInternalModifier,
        IAbstractModifier,
        IDotnetImportTypeModifier,
        
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
    public DotnetImportTypeData? DotnetImport { get; set; }
    
    public TypeReference? Extends { get; set; }
    public (FunctionObject parent, FunctionObject? overrided, bool isSealed)[]? VirtualTable { get; set; }
    
    public Alignment? Length { get; set; }
    public Alignment? Alignment { get; set; }

    public List<FieldObject> Fields { get; } = [];
    public List<ConstructorObject> Constructors { get; } = [];
    public List<DestructorObject> Destructors { get; } = [];
    public List<FunctionGroupObject> Functions { get; } = [];
    
    public readonly StructureDeclarationNode SyntaxNode = synNode;

    public override LangObject? SearchChild(string name)
        => Fields.FirstOrDefault(e => e.Name == name)
           ?? (LangObject?)Functions.FirstOrDefault(e => e.Name == name);

    public override string ToString()
    {
        var sb = new StringBuilder();
        
        sb.Append(Public ? "public " : "private ");
        sb.Append(Static ? "static " : "instance ");
        if (Internal) sb.Append("internal ");
        sb.Append(Abstract ? "abstract " : "concrete ");

        sb.Append($"struct '{Name}'");
        if (Extends != null) sb.Append($" extends {Extends:sig}");
        sb.AppendLine(" {");
        
        foreach (var c in Fields) sb.AppendLine(c.ToString().TabAll());
        foreach (var c in Functions) sb.AppendLine(c.ToString().TabAll());

        sb.AppendLine("}");
        return sb.ToString();
    }
    public override string ToSignature() => $"{Name}";
}
