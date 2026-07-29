using System.Text;
using Abstract.CodeProcess.Core;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IrNewObject(SyntaxNode origin, IrReference type, IrExpression[] args, IrAssign[] inlineAssigns) : IrExpression(origin)
{
    public IrExpression[] Arguments = args;
    public IrAssign[] InlineAssignments = inlineAssigns;

    public ITypeReference InstanceType = null!;
    public ITypeReference? OverrideReturnType = null;
    public IrReference Target = type;
    
    public override ITypeReference Type => OverrideReturnType ?? InstanceType;
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
