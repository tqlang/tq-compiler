using System.Text;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expresions;

public class IRInvoke(
    FunctionCallExpressionNode origin,
    IrExpression target,
    IrExpression[] args) : IrExpression(origin)
{
    public override TypeReference Type => ((FunctionTypeReference?)Target.Type)?.Returns ?? null!;

    public IrExpression Target { get; set; } = target;
    public IrExpression[] Arguments { get; set; } = args;

    public override string ToString() => $"call {Target} ({string.Join(", ", Arguments)})";
}
