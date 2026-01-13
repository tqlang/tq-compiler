using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.Attributes;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.CodeObjects;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;

public class DestructorObject(DestructorDeclarationNode synNode) : LangObject("destructor"),
        IDotnetImportMethodModifier
{
    public DotnetImportMethodData? DotnetImport { get; set; }
    
    public ParameterObject[] Parameters => [.. _parameters];
    public LocalVariableObject[] Locals => [.._locals];
    public IRBlock? Body = null;
    
    public readonly DestructorDeclarationNode SyntaxNode = synNode;
    private readonly List<ParameterObject> _parameters = [];
    private readonly List<LocalVariableObject> _locals = [];

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
        sb.Append($"dtor({string.Join(", ", _parameters.Select(e => e.Type))})");

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
    public override string ToSignature() => $"{Parent:sig}" +
                                   $".dtor({string.Join(", ", _parameters.Select(e => e.Type))})";
}
