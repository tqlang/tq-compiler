using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expresions;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;

public class IRBooleanLiteral(SyntaxNode origin, bool value): IrExpression(origin)
{
    public override TypeReference Type => new BooleanTypeReference();
    public readonly bool Value = value;
    public override string ToString() => Value ? "true" : "false";
}