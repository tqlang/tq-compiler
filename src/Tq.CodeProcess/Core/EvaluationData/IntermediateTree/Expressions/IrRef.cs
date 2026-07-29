using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrRef(SyntaxNode origin, IrExpression v) : IrExpression(origin)
{
    public readonly IrExpression Expression = v;
    public override string ToString() => $"&{Expression}";
    public override ITypeReference Type => new ReferenceTypeReference((Reference)Expression.Type);
}
