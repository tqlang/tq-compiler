using System.Text;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Statements;

public class IrReturn(SyntaxNode origin, IrExpression? val) : IRStatement(origin)
{
    public IrExpression? Value { get; set; } = val;
    
    public override string ToString()
    {
        if (Value == null) return "(ret)";
        var sb = new StringBuilder();

        sb.Append($"ret ");
        sb.AppendLine(Value.ToString().TabAll()[1..]);
        
        return sb.ToString();
    }
}
