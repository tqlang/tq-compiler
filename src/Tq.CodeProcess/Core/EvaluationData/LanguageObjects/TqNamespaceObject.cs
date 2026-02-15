using System.Text;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects.Containers;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class TqNamespaceObject(string n, NamespaceNode synNode) : BaseNamespaceObject(n),
    INamespaceContainer,
    IFieldContainer,
    IFunctionContainer,
    IStructContainer,
    ITypedefContainer
{
    public readonly NamespaceNode SyntaxNode = synNode;
    private List<FieldObject> _fields;
    private List<FunctionGroupObject> _functions;
    private List<StructObject> _structs;
    private List<TypedefObject> _typedefs;

    public List<SourceScript> Scripts { get; } = [];
    public List<BaseNamespaceObject> Namespaces { get; } = [];
    public List<FieldObject> Fields { get; } = [];
    public List<FunctionGroupObject> Functions { get; } = [];
    public List<StructObject> Structs { get; } = [];
    public List<TypedefObject> Typedefs { get; } = [];

    
    public override LangObject? SearchChild(string name)
        => Fields.FirstOrDefault(e => e.Name == name)
            ?? (LangObject?)Structs.FirstOrDefault(e => e.Name == name)
            ?? (LangObject?)Typedefs.FirstOrDefault(e => e.Name == name)
            ?? Functions.FirstOrDefault(e => e.Name == name);

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Namespace '{Name}' ('{string.Join('.', Global)}') {{");

        foreach (var i in Fields) sb.AppendLine($"{i}".TabAll());
        foreach (var i in Structs) sb.AppendLine($"{i}".TabAll());
        foreach (var i in Typedefs) sb.AppendLine($"{i}".TabAll());
        foreach (var i in Functions) sb.AppendLine($"{i}".TabAll());

        sb.AppendLine("}");
        return sb.ToString();
    }
    public override string ToSignature() => $"Namespace {string.Join('.', Global)}";
}
