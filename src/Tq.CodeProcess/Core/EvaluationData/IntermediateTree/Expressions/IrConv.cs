using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrConv(SyntaxNode origin, IrExpression v, ITypeReference ty) : IrExpression(origin)
{
    public override ITypeReference Type => ty;
    public ITypeReference OriginType => Expression.Type;
    public IrExpression Expression = v;

    public override string ToString() => $"{Expression} as {Type}";
}
