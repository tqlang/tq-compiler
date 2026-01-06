using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expresions;

public class IRIntCast(SyntaxNode origin, IrExpression v, IntegerTypeReference ty) : IrExpression(origin)
{
    public override TypeReference Type => IntType;
    public readonly IntegerTypeReference IntType = ty;
    public IntegerTypeReference TargetType => (IntegerTypeReference)v.Type;
    
    public IrExpression Expression = v;
    
    public override string ToString() => $"{Expression} intcast {Type}";
}