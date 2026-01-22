namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

public class FunctionTypeReference(TypeReference? returns, TypeReference[] parameters) : BuiltInTypeReference
{
    public readonly TypeReference Returns = returns ?? new VoidTypeReference();
    public readonly TypeReference[] Parameters = parameters;
    
    public override Alignment Length => 0;
    public override Alignment Alignment => 0;

    public override bool IsGeneric => Returns.IsGeneric || Parameters.Any(e => e.IsGeneric);

    public override string ToString() => $"fn({string.Join(", ", Parameters.Select(e => e.ToString()))}) {Returns}";
}
