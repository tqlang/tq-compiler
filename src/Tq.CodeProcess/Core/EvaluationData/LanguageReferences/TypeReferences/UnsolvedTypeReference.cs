using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

public class UnsolvedTypeReference(ExpressionNode node) : TypeReference
{
    public readonly ExpressionNode SyntaxNode = node;
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;
    public override string ToString() => $"UType<{SyntaxNode}>";
}
