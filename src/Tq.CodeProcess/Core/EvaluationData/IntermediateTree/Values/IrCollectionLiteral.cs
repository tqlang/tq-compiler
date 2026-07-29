using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IrCollectionLiteral(SyntaxNode origin, Reference elementType, IrExpression[] items): IrExpression(origin)
{
    public override ITypeReference Type => new SliceTypeReference(ElementType);
    public readonly Reference ElementType = elementType;
    
    public readonly IrExpression[] Items = items;
    public int Length => Items.Length;
    
    public override string ToString() => $"{Type} [{string.Join(", ", Items)}]";
}
