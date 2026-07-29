using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IRBooleanLiteral(SyntaxNode origin, bool value): IrExpression(origin)
{
    public override ITypeReference Type => new BooleanTypeReference();
    public readonly bool Value = value;
    public override string ToString() => Value ? "true" : "false";
}