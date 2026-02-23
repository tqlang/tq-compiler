using System.Text;
using AsmResolver.DotNet;
using TypeReference = Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.TypeReference;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

public class DotnetFieldObject(FieldDefinition field, TypeReference t) : LangObject(null!, field.Name!)
{
    public readonly FieldDefinition Reference = field;
    public readonly TypeReference FieldType = t;
    
    public bool IsConstant => Reference.Constant != null;
    public bool IsStatic => Reference.Constant is null;
    
    public override string ToString()
    {
        var isConstant = Reference.Constant == null;
        var sb = new StringBuilder();
        
        sb.Append(Reference.IsStatic ? "static " : "instance ");
        sb.Append("field ");
        if (isConstant) sb.Append("constant ");
        sb.Append(Reference.Signature!.FieldType);
        sb.Append(' ');
        sb.Append(Reference.Name);

        if (isConstant)
        {
            sb.Append(" = ");
            sb.Append(Reference.Constant!.Value);
        }
        
        return sb.ToString();
    }
    public override string ToSignature() => (Reference.Constant != null ? "field" : "const") + $"  {Reference.Name}";
}