using System.Text;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Attributes;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class TypedefObject(SourceScript script, string n, TypeDefinitionNode synNode) : LangObject(script, n),
    IFunctionContainer,
    IPublicModifier,
    IStaticModifier,
    IInternalModifier,
    IAbstractModifier
{
    bool IPublicModifier.Public { get; set; } = false;
    bool IStaticModifier.Static { get; set; } = false;
    bool IInternalModifier.Internal { get; set; } = false;
    bool IAbstractModifier.Abstract { get; set; } = false;

    public TypeReference? BackType = null;
    
    public readonly TypeDefinitionNode syntaxNode = synNode;
    public List<TypedefNamedValue> NamedValues = [];
    public List<FunctionGroupObject> Functions { get; } = [];


    public override LangObject? SearchChild(string name, SearchChildMode mode)
    {
        return mode switch
        {
            SearchChildMode.All or SearchChildMode.OnlyStatic
                => NamedValues.FirstOrDefault(x => x.Name == name)
                   ?? (LangObject?)Functions.FirstOrDefault(x => x.Name == name),
            
            SearchChildMode.OnlyInstance
                => Functions.FirstOrDefault(x => x.Name == name),
            
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (BackType == null) sb.AppendLine($"typedef '{Name}' {{");
        else sb.AppendLine($"typedef({BackType}) '{Name}' {{");

        foreach (var (i, e) in NamedValues.Index())
            sb.AppendLine($"\tcase {e}" + (i < NamedValues.Count ? $"," : ""));
        
        foreach (var c in Functions) sb.AppendLine(c.ToString().TabAll());

        sb.AppendLine("}");
        return sb.ToString();
    }
    public override string ToSignature() => $"{Name}";
}
