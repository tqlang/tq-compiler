using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class FunctionTypeReference(Reference? returns, Reference[] parameters) : BuiltInTypeReference
{
    public readonly Reference Returns = returns ?? new VoidTypeReference();
    public readonly Reference[] Parameters = parameters;
    
    public override ITypeReference Type => new TypeTypeReference(this);
    
    public override string ToString() => $"fn({string.Join(", ", Parameters.Select(e => e.ToString()))}) {Returns}";
}
