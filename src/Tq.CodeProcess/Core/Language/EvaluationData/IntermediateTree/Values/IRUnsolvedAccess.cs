using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;

public class IrImplicitAccess(SyntaxNode origin) : IRReference(origin) {
    public override TypeReference Type => ImplicitType;
    public TypeReference ImplicitType = new UnsolvedTypeReference(null!);
        
    public override string ToString() => $"<{ImplicitType}>";
}
