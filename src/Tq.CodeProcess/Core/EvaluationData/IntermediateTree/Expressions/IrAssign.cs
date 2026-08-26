using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrAssign(
    AssignmentExpressionNode origin, 
    IrExpression targ,
    IrExpression val
    ) : IrExpression(origin)
{
    public IrExpression Target { get; set; } = targ;
    public IrExpression Value { get; set; } = val;
    public override ITypeReference Type => Target.Type;

    public override string ToString() => $"{Target} = {Value}";
}
