using System.Numerics;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expressions;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;

public class IrCollectionLiteral(SyntaxNode origin, TypeReference elementType, IrExpression[] items): IrExpression(origin)
{
    public override TypeReference Type => new SliceTypeReference(ElementType);
    public readonly TypeReference ElementType = elementType;
    
    public readonly IrExpression[] Items = items;
    public int Length => items.Length;
    
    public override string ToString() => $"{Type}[{string.Join(", ", Items)}]";
}
