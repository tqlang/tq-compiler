using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Statements;

public class IRIf(SyntaxNode origin, IrExpression exp, IRBlock then) : IRStatement(origin), IIfElse
{
    public IrExpression Condition = exp;
    public IRBlock Then = then;
    public IIfElse? Else = null;

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"if ({Condition}) {{");
        sb.Append(Then.ToString().TabAll());
        sb.Append('}');

        if (Else is IRIf @irif)
        {
            sb.Append(" else ");
            sb.Append(irif);
        }
        else if (Else is IRElse)
        {
            sb.AppendLine(" else {");
            sb.Append(Else.ToString().TabAll());
            sb.Append('}');
        }

        sb.AppendLine();
        return sb.ToString();
    }
}
