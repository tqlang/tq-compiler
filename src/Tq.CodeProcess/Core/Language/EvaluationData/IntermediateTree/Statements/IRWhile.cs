using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expresions;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Statements;

public class IRWhile(SyntaxNode origin, IRBlock?  d, IrExpression? c, IRBlock? s, IRBlock p) : IRStatement(origin)
{
    public IRBlock? Define = d;
    public IrExpression Condition = c;
    public IRBlock? Step = s;
    public IRBlock Process = p;

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
