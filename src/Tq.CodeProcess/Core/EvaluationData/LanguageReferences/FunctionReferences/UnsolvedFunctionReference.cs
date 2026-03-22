using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;

public class UnsolvedFunctionReference(SyntaxNode node): FunctionReference
{
    public readonly SyntaxNode Expression = node;
    public override TypeReference Type => null!;

    public override string ToString() => $"UFun({Expression})";
}
