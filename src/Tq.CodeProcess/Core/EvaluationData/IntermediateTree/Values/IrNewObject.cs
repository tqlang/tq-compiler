using System.Text;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IrNewObject(SyntaxNode origin, IrReference type, IrExpression[] args, IRAssign[] inlineAssigns) : IrExpression(origin)
{
    public IrExpression[] Arguments = args;
    public IRAssign[] InlineAssignments = inlineAssigns;

    public StructObject InstanceType = null!;
    public IrReference Target = type;
    
    public override TypeReference Type => ((FunctionTypeReference)Target?.Type!).Returns;
    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append($"new ");
        sb.Append(Target);
        sb.Append($"({string.Join(", ", Arguments)})");

        if (InlineAssignments.Length <= 0) return sb.ToString();
        
        sb.AppendLine("{");
        foreach (var i in InlineAssignments) sb.Append(i.ToString().TabAll());
        sb.AppendLine("}");

        return sb.ToString();
    }
}
