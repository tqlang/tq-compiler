using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin.Integer;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;

public class SliceCallReference(StringEncoding encoding): FunctionReference
{
    public override TypeReference FieldType => new FunctionTypeReference(
        new StringTypeReference(encoding),
        [
            new StringTypeReference(encoding),
            new RuntimeIntegerTypeReference(false),
            new RuntimeIntegerTypeReference(false),
        ]);
    public override string ToString() => $"@slice";
}
