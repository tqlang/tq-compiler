using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrTernary(SyntaxNode origin, IrExpression condition, IrExpression ifTrue, IrExpression ifFalse) : IrExpression(origin)
{
    public IrExpression Condition = condition;
    public IrExpression TrueExpression = ifFalse;
    public IrExpression FalseExpression = ifFalse;
    public ITypeReference ExpressionType = null!;

    public override string ToString() => $"{condition} ? {TrueExpression} : {FalseExpression}";
    public override ITypeReference Type => ExpressionType;
}
