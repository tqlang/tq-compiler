using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;

public class UnsolvedTypeReference(ExpressionNode node) : TypeReference
{
    public readonly ExpressionNode syntaxNode = node;
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;
    public override string ToString() => $"UType<{syntaxNode}>";
}
