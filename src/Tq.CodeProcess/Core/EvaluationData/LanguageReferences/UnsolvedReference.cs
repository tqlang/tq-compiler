using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;

public class UnknownReference(ExpressionNode node) : Reference
{
    public readonly ExpressionNode SyntaxNode = node;
    public override ITypeReference Type => null!;
    public override bool IsSolved => false;
    
    public override string ToString() => $"U<{SyntaxNode}>";
}
