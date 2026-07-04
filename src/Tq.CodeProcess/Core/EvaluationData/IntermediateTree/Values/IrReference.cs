using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IrReference : IrExpression
{
    public readonly Reference Reference;
    public override ITypeReference Type => Reference.Type;
    public bool IsSolved => Reference.IsSolved;

    public IrReference(SyntaxNode origin, Reference reference) : base(origin) => Reference = reference;
    public IrReference(SyntaxNode origin) : base(origin) => Reference = new UnknownReference((ExpressionNode)origin);
    
    public override string ToString() => Reference.ToString() ?? throw new NotImplementedException();
}
