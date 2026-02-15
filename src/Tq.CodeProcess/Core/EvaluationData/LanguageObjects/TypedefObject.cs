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
    IAbstractModifier,
    IDotnetImportTypeModifier
{
    bool IPublicModifier.Public { get; set; } = false;
    bool IStaticModifier.Static { get; set; } = false;
    bool IInternalModifier.Internal { get; set; } = false;
    bool IAbstractModifier.Abstract { get; set; } = false;
    public DotnetImportTypeData? DotnetImport { get; set; }

    public TypeReference? BackType = null;
    
    public readonly TypeDefinitionNode syntaxNode = synNode;
    public List<TypedefNamedValue> NamedValues = [];
    public List<FunctionGroupObject> Functions { get; } = [];


    public override LangObject? SearchChild(string name)
    {
        return NamedValues.FirstOrDefault(x => x.Name == name)
            ?? (LangObject?)Functions.FirstOrDefault(x => x.Name == name);
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        if (BackType == null) sb.AppendLine($"typedef '{Name}' {{");
        else sb.AppendLine($"typedef({BackType}) '{Name}' {{");

        sb.AppendLine(string.Join($",{Environment.NewLine}\t", NamedValues));
        foreach (var c in Functions) sb.AppendLine(c.ToString().TabAll());

        sb.AppendLine("}");
        return sb.ToString();
    }
    public override string ToSignature() => $"{Name}";
}
