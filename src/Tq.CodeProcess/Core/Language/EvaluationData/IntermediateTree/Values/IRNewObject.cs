using System.Diagnostics;
using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expresions;
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

        sb.AppendLine($"(new");
        sb.AppendLine((Type ?? throw new UnreachableException()).ToString().TabAll());
        
        foreach (var arg in Arguments)
            sb.Append(arg.ToString().TabAll());
        
        sb.Append("\t(");

        foreach (var i in InlineAssignments)
            sb.Append(i.ToString().TabAll());
        
        sb.Append("))");
        
        return sb.ToString();
    }
}
