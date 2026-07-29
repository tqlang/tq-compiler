using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.FunctionReferences;

public class FunctionGroupReference(FunctionGroupObject fun) : FunctionReference
{
    public readonly FunctionGroupObject FunctionGroup = fun;
    public override ITypeReference Type => null!;

    public override string ToString() => $"FGr<{string.Join('.', FunctionGroup.Global)}>";
}
