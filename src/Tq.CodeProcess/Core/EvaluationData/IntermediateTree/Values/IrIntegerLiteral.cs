using System.Numerics;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IrIntegerLiteral(SyntaxNode origin, BigInteger val, IntegerTypeReference ty): IrExpression(origin)
{
    public override TypeReference Type => ty;
    public ushort? Size => (ushort)(Type as RuntimeIntegerTypeReference)!.BitSize.Bits;
    public readonly BigInteger Value = val;
    
    public override string ToString() => $"({Type}){Value}";
}
