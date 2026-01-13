using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects.Attributes;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Control;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageObjects;

public class FieldObject(string n, TopLevelVariableNode synNode, TypeReference t) : LangObject(n),
        IPublicModifier,
        IStaticModifier,
        IInternalModifier,
        IAbstractModifier
{
    public bool Constant { get; set; } = false;
    public bool Public { get; set; } = false;
    public bool Static { get; set; } = false;
    public bool Internal { get; set; } = false;
    public bool Abstract { get; set; } = false;
    
    public TypeReference Type { get; set; } = t;
    
    public readonly TopLevelVariableNode SyntaxNode = synNode;

    public Alignment? Offset { get; set; }
    public Alignment Length => Type.Length;
    public Alignment Alignment => Type.Alignment;

    public IrExpression? Value = null;
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        
        if (Offset.HasValue) sb.AppendLine($"; offset={Offset.Value}");
        sb.Append(Public ? "public " : "private ");
        sb.Append(Static ? "static " : "instance ");
        if (Internal) sb.Append("internal ");
        sb.Append(Abstract ? "abstract " : "concrete ");

        sb.Append(Constant ? "const " : "var ");
        sb.AppendLine($"'{Name}' {Type:sig}");

        if (Value != null)
        {
            sb.Append($" = {Value}");
        }
        
        return sb.ToString();
    }

    public override string ToSignature() => $"field {Type:sig} {Name}";
}
