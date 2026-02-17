using System.Text;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Attributes;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;
using TypeReference = Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.TypeReference;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class FunctionObject(SourceScript sourceScript, string n, FunctionDeclarationNode synNode) : LangObject(sourceScript, n),
        IPublicModifier,
        IStaticModifier,
        IInternalModifier,
        IAbstractModifier,
        IVirtualModifier,
        IOverrideAttribute,
        IExternModifier,
        IExportModifier,
        ICallable
{
    public readonly FunctionDeclarationNode SyntaxNode = synNode;
    
    public bool Public { get; set; } = false;
    public bool Static { get; set; } = false;
    public bool Internal { get; set; } = false;
    public bool Abstract { get; set; } = false;
    public bool Virtual { get; set; } = false;
    public bool Override { get; set; } = false;
    public string? Export { get; set; } = null;
    public bool IsGeneric { get; set; } = false;
    public bool ConstExp { get; set; } = false;
    public (string nmsp, string name)? Extern { get; set; } = null;

    SourceScript ICallable.Script => SourceScript;
    public FunctionGroupObject ParentGroup { get; internal set; } = null!;
    public List<ParameterObject> Parameters { get; } = [];
    public List<LocalVariableObject> Locals { get; } = [];
    public TypeReference ReturnType { get; set; } = null!;
    public IrBlock? Body { get; set; }
    
    bool ICallable.IsStatic => Static;
    bool ICallable.IsGeneric => Parameters.Any(p => p.IsGeneric);
    
    public void AddParameter(params ParameterObject[] parameter)
    {
        var lastidx = Parameters.Count;
        foreach (var p in parameter)
        {
            Parameters.Add(p);
            p.Index = lastidx++;
        }
    }
    public void AddLocal(params LocalVariableObject[] local)
    {
        var lastidx = Locals.Count;
        foreach (var p in local)
        {
            Locals.Add(p);
            p.index = lastidx++;
        }
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append(Public ? "public " : "private ");
        sb.Append(Static ? "static " : "instance ");
        if (Internal) sb.Append("internal ");
        sb.Append(Abstract ? "abstract " : "concrete ");
        if (Virtual) sb.Append("virtual ");
        if (Override) sb.Append("override ");
        if (IsGeneric) sb.Append("generic ");

        sb.Append($"func {Name}({string.Join(", ", Parameters.Select(e => e.Type))}) {ReturnType}");
        if (Body != null)
        {
            sb.AppendLine(" {");
            foreach (var l in Locals) sb.AppendLine($"\t{l}");
            sb.AppendLine(Body.ToString().TabAll());
            sb.AppendLine("}");
        }
        else sb.AppendLine();
        
        return sb.ToString();
    }
    public override string ToSignature() => $"{Name}({string.Join(", ", Parameters.Select(e => e.Type))}) {ReturnType}";
}
