using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IRAccess(SyntaxNode origin, IrExpression a, IrExpression b) : IrReference(origin) {
    public IrExpression A = a;
    public IrExpression B = b;

    public override TypeReference Type => B.Type;

    public override string ToString() => $"{A}->{B}";
}
