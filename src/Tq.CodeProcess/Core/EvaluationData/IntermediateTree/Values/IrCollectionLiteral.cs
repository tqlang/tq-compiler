using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IrCollectionLiteral(SyntaxNode origin, Reference elementType, IrExpression[] items): IrExpression(origin)
{
    public override ITypeReference Type => new SliceTypeReference(ElementType);
    public readonly Reference ElementType = elementType;
    
    public readonly IrExpression[] Items = items;
    public int Length => Items.Length;
    
    public override string ToString() => $"{Type} [{string.Join(", ", Items)}]";
}
