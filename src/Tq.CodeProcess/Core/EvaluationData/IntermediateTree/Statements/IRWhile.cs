using System.Text;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Statements;

public class IRWhile(SyntaxNode origin, IrBlock?  d, IrExpression? c, IrBlock? s, IrBlock p) : IRStatement(origin)
{
    public IrBlock? Define = d;
    public IrExpression Condition = c;
    public IrBlock? Step = s;
    public IrBlock Process = p;

    public override string ToString()
    {
        var sb = new StringBuilder();
        
        if (Define != null) sb.AppendLine($"{{ {Define} }}");
        sb.AppendLine($"while ({Condition.ToString().TabAll()[1..]}) {{");
        
        sb.AppendLine($"{Process.ToString().TabAll()}");
        
        if (Step != null) sb.AppendLine($"{{ {Step} }}".TabAll());
        sb.AppendLine("}");
        
        return sb.ToString();
    }
}
