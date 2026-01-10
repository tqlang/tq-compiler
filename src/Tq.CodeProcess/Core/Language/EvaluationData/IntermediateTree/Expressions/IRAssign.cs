using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Statements;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;

public class IRAssign(
    AssignmentExpressionNode origin, 
    IrExpression targ,
    IrExpression val
    ) : IrExpression(origin)
{
    public IrExpression Target { get; set; } = targ;
    public IrExpression Value { get; set; } = val;
    public override TypeReference Type => Target.Type;

    public override string ToString() => $"{Target} = {Value}";
}
