using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.Attributes;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.Metadata;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;
using TypeReference = Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.TypeReference;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;

public class FunctionObject(string n, FunctionDeclarationNode synnode) : LangObject(n),
        IPublicModifier,
        IStaticModifier,
        IInternalModifier,
        IAbstractModifier,
        IVirtualModifier,
        IOverrideAttribute,
        IExternModifier,
        IExportModifier,
        IDotnetImportMethodModifier,
        IParametrizable
{
    public bool Public { get; set; } = false;
    public bool Static { get; set; } = false;
    public bool Internal { get; set; } = false;
    public bool Abstract { get; set; } = false;
    public bool Virtual { get; set; } = false;
    public bool Override { get; set; } = false;
    public string? Export { get; set; } = null;
    public bool Generic { get; set; } = false;
    public bool ConstExp { get; set; } = false;
    public (string nmsp, string name)? Extern { get; set; } = null;
    public DotnetImportMethodData? DotnetImport { get; set; }

    
    public TypeReference ReturnType = null!;
    public ParameterObject[] Parameters => [.. _parameters];
    public LocalVariableObject[] Locals => [.._locals];
    public IRBlock? Body = null;
    

    public readonly FunctionDeclarationNode syntaxNode = synnode;
    private List<ParameterObject> _parameters = [];
    private List<LocalVariableObject> _locals = [];

    public void AddParameter(params ParameterObject[] parameter)
    {
        var lastidx = _parameters.Count;
        foreach (var p in parameter)
        {
            _parameters.Add(p);
            p.index = lastidx++;
        }
    }
    public void AddLocal(params LocalVariableObject[] local)
    {
        var lastidx = _locals.Count;
        foreach (var p in local)
        {
            _locals.Add(p);
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
        if (Extern == null) sb.Append($"extern(\"{Extern}\") ");
        if (Generic) sb.Append("generic ");

        sb.Append($"func {Name}({string.Join(", ", _parameters.Select(e => e.Type))}) {ReturnType}");
        if (Body != null)
        {
            sb.AppendLine(" {");
            foreach (var l in _locals) sb.AppendLine($"\t{l}");
            sb.AppendLine(Body.ToString().TabAll());
            sb.AppendLine("}");
        }
        else sb.AppendLine();
        
        return sb.ToString();
    }
    public override string ToSignature() => $"{Name}({string.Join(", ", Parameters.Select(e => e.Type))}) {ReturnType}";
}
