using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrCall(
    SyntaxNode origin,
    IrExpression target,
    IrExpression[] args) : IrExpression(origin)
{
    public override ITypeReference Type => ((FunctionTypeReference?)Target.Type)?.Returns as ITypeReference ?? null!; 

    public IrExpression Target { get; set; } = target;
    public IrExpression[] Arguments { get; set; } = args;

    public override string ToString()
        => $"call " +
           Target switch
           {
               IrReference { IsSolved: true, Reference: CallableReference @fb } => string.Join('.', ((LangObject)fb.Callable).Global),
               _ => Target.ToString()
           } +
        $" ({string.Join(", ", Arguments.Select(e => e?.ToString() ?? "<nil>"))})";
}
