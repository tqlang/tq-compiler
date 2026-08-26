using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree;
using Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Statements;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.Language.SyntaxNodes;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrPatternMatch(SyntaxNode origin) : IrExpression(origin)
{
    private ITypeReference _type = null!;

    public IrExpression Expression = null!;
    public List<IrPatternMatchCase> Cases = [];
    public IrPatternMatchCase? Default = null;

    public override ITypeReference Type => _type;
}

public class IrPatternMatchCase(SyntaxNode origin) : IrNode(origin)
{
    public IrExpression? Pattern;
    public IrNode? Action;
}
