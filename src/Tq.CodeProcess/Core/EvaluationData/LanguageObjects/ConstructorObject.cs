using System.Text;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Attributes;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class ConstructorObject(SourceScript sourceScript, ConstructorDeclarationNode synNode) : LangObject(sourceScript, "constructor"),
        IDotnetImportMethodModifier,
        ICallable
{
    public readonly ConstructorDeclarationNode SyntaxNode = synNode;
    
    public DotnetImportMethodData? DotnetImport { get; set; }
    public TypeReference? ReturnTypeOverride = null;

    SourceScript ICallable.Script => SourceScript;
    public List<ParameterObject> Parameters { get; } = [];
    public List<LocalVariableObject> Locals { get; } = [];
    public TypeReference ReturnType => ReturnTypeOverride ?? new SolvedStructTypeReference((StructObject)Parent);
    public IrBlock? Body { get; set; }
    
    bool ICallable.IsStatic => false;
    bool ICallable.IsGeneric => Parameters.Any(p => p.IsGeneric);
    
    public void AddParameter(params ParameterObject[] parameter)
    {
        var i = Parameters.Count;
        foreach (var p in parameter)
        {
            Parameters.Add(p);
            p.Index = i++;
        }
    }
    public void AddLocal(params LocalVariableObject[] local)
    {
        var i = Locals.Count;
        foreach (var p in local)
        {
            Locals.Add(p);
            p.index = i++;
        }
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"ctor({string.Join(", ", Parameters.Select(e => e.Type))})");
        if (ReturnType is not SolvedStructTypeReference) sb.AppendLine($" {ReturnType}");
        
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
    public override string ToSignature() => $"{Parent:sig}.ctor({string.Join(", ", Parameters.Select(e => e.Type))})";
}
