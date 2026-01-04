using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expresions;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;

public class IRAccess(SyntaxNode origin, IrExpression a, IrExpression b) : IRReference(origin) {
    public IrExpression A = a;
    public IrExpression B = b;

    public override TypeReference Type => B.Type;

    public override string ToString() => $"{A}->{B}";
}
