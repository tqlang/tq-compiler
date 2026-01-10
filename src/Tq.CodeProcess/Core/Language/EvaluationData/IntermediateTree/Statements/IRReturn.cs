using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Statements;

public class IRReturn(SyntaxNode origin, IrExpression? val) : IRStatement(origin)
{
    public IrExpression? Value { get; set; } = val;
    
    public override string ToString()
    {
        if (Value == null) return "(ret)";
        var sb = new StringBuilder();

        sb.AppendLine($"(ret");
        sb.AppendLine(Value.ToString().TabAll());
        sb.Append(')');
        
        return sb.ToString();
    }
}
