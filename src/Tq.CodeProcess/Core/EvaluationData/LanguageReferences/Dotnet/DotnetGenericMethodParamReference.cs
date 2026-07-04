using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;

public class DotnetGenericMethodParamReference(int index) : Reference, ITypeReference
{
    public readonly int Index = index;
    
    public override string ToString() => $"!!{Index}";
    public override ITypeReference Type => new TypeTypeReference(this);
    public bool IsGeneric => true;
}