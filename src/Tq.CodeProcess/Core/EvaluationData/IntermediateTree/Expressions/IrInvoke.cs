using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrInvoke(
    FunctionCallExpressionNode origin,
    IrExpression target,
    IrExpression[] args) : IrExpression(origin)
{
    public override TypeReference Type => ((FunctionTypeReference?)Target.Type)?.Returns ?? null!;

    public IrExpression Target { get; set; } = target;
    public IrExpression[] Arguments { get; set; } = args;

    public override string ToString() => $"call {Target} ({string.Join(", ", Arguments.Select(e => e?.ToString() ?? "<nil>"))})";
}
