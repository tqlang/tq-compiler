using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.Attributes;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.Containers;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;

public class TypedefObject(string n, TypeDefinitionNode synNode) : LangObject(n),
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
    
    
    public readonly TypeDefinitionNode syntaxNode = synNode;
    public List<TypedefNamedValue> NamedValues = [];
    public List<FunctionGroupObject> Functions { get; } = [];
    
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"typedef '{Name}' {{");

        sb.AppendLine(string.Join($",{Environment.NewLine}\t", NamedValues));
        foreach (var c in Functions) sb.AppendLine(c.ToString().TabAll());

        sb.AppendLine("}");
        return sb.ToString();
    }
    public override string ToSignature() => $"{Name}";
}
