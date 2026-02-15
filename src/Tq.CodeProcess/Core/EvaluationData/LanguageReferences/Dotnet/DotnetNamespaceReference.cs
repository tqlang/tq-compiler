using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.Dotnet;

public class DotnetNamespaceReference(DotnetNamespaceObject nmsp) : LanguageReference
{
    public readonly DotnetNamespaceObject Nmsp = nmsp;
    public override TypeReference Type => new TypeTypeReference(null);
    public override string ToString() => Nmsp.Name;
}