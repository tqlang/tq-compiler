using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IrCharLiteral(SyntaxNode origin, char data) : IrExpression(origin)
{
    public readonly char Data = data;
    
    public override TypeReference Type => new CharTypeReference();
    public StringEncoding Encoding => ((StringTypeReference)Type!).Encoding;
    
    public override string ToString() => $"(char) '{Data}'";
}
