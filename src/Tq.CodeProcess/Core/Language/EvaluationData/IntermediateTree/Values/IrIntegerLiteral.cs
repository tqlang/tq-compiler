using System.Numerics;
using Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Expresions;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.Language.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.Language.EvaluationData.IntermediateTree.Values;

public class IrIntegerLiteral(SyntaxNode origin, BigInteger val, IntegerTypeReference ty): IrExpression(origin)
{
    public override TypeReference Type => ty;
    public ushort? Size => (Type as RuntimeIntegerTypeReference)?.BitSize ?? 0;
    public readonly BigInteger Value = val;
    
    
    public override string ToString() => $"{Value}";
}
