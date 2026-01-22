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
        
        sb.AppendLine($"while ({Condition.ToString().TabAll()[1..]}) {{");
        
        if (Define != null) sb.Append($"def {{ {Define.ToString()} }}".TabAll());
        if (Step != null) sb.Append($"step {{ {Step.ToString()} }}".TabAll());
        
        sb.Append($"{Process.ToString().TabAll()}");
        sb.AppendLine("}");
        
        return sb.ToString();
    }
}
