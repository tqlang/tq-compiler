using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IrImplicitAccess(SyntaxNode origin) : IrReference(origin) {
    public override TypeReference Type => ImplicitType;
    public TypeReference ImplicitType = new UnsolvedTypeReference(null!);
        
    public override string ToString() => $"<{ImplicitType}>";
}
