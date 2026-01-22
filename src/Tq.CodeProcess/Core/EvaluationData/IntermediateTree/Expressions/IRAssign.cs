using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

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
