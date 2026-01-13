using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;

public class IRUnknownReference(SyntaxNode origin) : IRReference(origin)
{
    public override TypeReference Type => null!;
    public override string ToString() => $"Unknown({Origin})";
}
