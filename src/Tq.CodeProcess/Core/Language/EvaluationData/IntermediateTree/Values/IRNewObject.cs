using System.Diagnostics;
using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;

public class IRNewObject(SyntaxNode origin, TypeReference t, IrExpression[] args, IRAssign[] inlineAssingns) : IrExpression(origin)
{
    public TypeReference InstanceType = t;
    public readonly IrExpression[] Arguments = args;
    public readonly IRAssign[] InlineAssignments = inlineAssingns;
    
    public override TypeReference Type => InstanceType;
    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append($"new ");
        sb.Append(Type ?? throw new UnreachableException());
        sb.Append($"({string.Join(", ", Arguments)})");

        if (InlineAssignments.Length <= 0) return sb.ToString();
        
        sb.AppendLine("{");
        foreach (var i in InlineAssignments) sb.Append(i.ToString().TabAll());
        sb.AppendLine("}");

        return sb.ToString();
    }
}
