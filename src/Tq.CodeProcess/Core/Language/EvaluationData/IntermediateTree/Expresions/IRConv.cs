using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expresions;

public class IrConv(SyntaxNode origin, IrExpression v, TypeReference ty) : IrExpression(origin)
{
    public override TypeReference Type => ty;
    public TypeReference OriginType => Expression.Type;
    public IrExpression Expression = v;

    public override string ToString() => $"{Expression} as {Type}";
}
