using System.Numerics;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;

namespace Tq.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IrIntegerLiteral(SyntaxNode origin, BigInteger val, IntegerTypeReference ty): IrExpression(origin)
{
    public override ITypeReference Type => ty;
    public ushort? Size => (ushort)(Type as RuntimeIntegerTypeReference)!.BitSize.Bits;
    public readonly BigInteger Value = val;
    
    public override string ToString() => $"({Type}){Value}";
}
