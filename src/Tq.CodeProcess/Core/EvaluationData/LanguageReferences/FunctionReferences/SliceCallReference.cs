using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;

public class SliceCallReference(StringEncoding encoding): FunctionReference
{
    public override ITypeReference Type => new FunctionTypeReference(
        new StringTypeReference(encoding),
        [
            new StringTypeReference(encoding),
            new RuntimeIntegerTypeReference(false),
            new RuntimeIntegerTypeReference(false),
        ]);
    public override string ToString() => $"@slice";
}
