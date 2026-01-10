using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Expression;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Value;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;

public class IRSolvedReference(SyntaxNode origin, LanguageReference refe) : IRReference(origin)
{
    public override TypeReference Type => refe.Type;
    public readonly LanguageReference Reference = refe;
    
    public override string ToString() => Reference.ToString() ?? throw new NotImplementedException();
}
