using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrTernary(SyntaxNode origin, IrExpression condition, IrExpression ifTrue, IrExpression ifFalse) : IrExpression(origin)
{
    public IrExpression Condition = condition;
    public IrExpression TrueExpression = ifFalse;
    public IrExpression FalseExpression = ifFalse;
    public TypeReference ExpressionType = null!;

    public override string ToString() => $"{condition} ? {TrueExpression} : {FalseExpression}";
    public override TypeReference Type => ExpressionType;
}
