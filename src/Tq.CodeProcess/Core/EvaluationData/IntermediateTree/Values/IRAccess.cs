using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IrAccess(SyntaxNode origin, IrExpression a, IrExpression b) : IrReference(origin) {
    public IrExpression A = a;
    public IrExpression B = b;

    public override ITypeReference Type => B.Type;

    public override string ToString() => $"{A}->{B}";
}
