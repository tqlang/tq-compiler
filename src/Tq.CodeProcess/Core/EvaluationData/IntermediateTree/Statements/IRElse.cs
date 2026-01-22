using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Statements;

public class IRElse(SyntaxNode origin, IrBlock then) : IRStatement(origin), IIfElse
{
    public IrBlock Then = then;

    public override string ToString() => Then.ToString();
}
