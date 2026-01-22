using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IrCollectionLiteral(SyntaxNode origin, TypeReference elementType, IrExpression[] items): IrExpression(origin)
{
    public override TypeReference Type => new SliceTypeReference(ElementType);
    public readonly TypeReference ElementType = elementType;
    
    public readonly IrExpression[] Items = items;
    public int Length => items.Length;
    
    public override string ToString() => $"{Type}[{string.Join(", ", Items)}]";
}
