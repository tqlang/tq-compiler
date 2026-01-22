using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IRSolvedReference(SyntaxNode origin, LanguageReference refe) : IrReference(origin)
{
    public override TypeReference Type => refe.Type;
    public readonly LanguageReference Reference = refe;
    
    public override string ToString() => Reference.ToString() ?? throw new NotImplementedException();
}
