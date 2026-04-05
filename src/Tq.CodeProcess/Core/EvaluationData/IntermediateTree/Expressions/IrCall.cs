using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;
using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrCall(
    SyntaxNode origin,
    IrExpression target,
    IrExpression[] args) : IrExpression(origin)
{
    public override TypeReference Type => ((FunctionTypeReference?)Target.Type)?.Returns ?? null!;

    public IrExpression Target { get; set; } = target;
    public IrExpression[] Arguments { get; set; } = args;

    public override string ToString()
        => $"call " +
           Target switch
           {
               IrSolvedReference { Reference: SolvedCallableReference @fb } => string.Join('.', ((LangObject)fb.Callable).Global),
               _ => Target.ToString()
           } +
        $" ({string.Join(", ", Arguments.Select(e => e?.ToString() ?? "<nil>"))})";
}
