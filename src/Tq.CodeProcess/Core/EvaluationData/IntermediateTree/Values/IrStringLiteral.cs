using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Values;

public class IrStringLiteral : IrExpression
{
    private TypeReference _type;
    public readonly string Data;
    
    public override TypeReference Type => _type;
    public StringEncoding Encoding => ((StringTypeReference)Type!).Encoding;

    public IrStringLiteral(SyntaxNode origin, StringEncoding encoding, string data) : base(origin)
    {
        _type = new StringTypeReference(encoding);
        Data = data;
    }
    public IrStringLiteral(SyntaxNode origin, string data) : base(origin)
    {
        _type = new StringTypeReference(StringEncoding.Undefined);
        Data = data;
    }

    public override string ToString() => $"({((StringTypeReference)Type!).Encoding}) \"{Data}\"";
}
