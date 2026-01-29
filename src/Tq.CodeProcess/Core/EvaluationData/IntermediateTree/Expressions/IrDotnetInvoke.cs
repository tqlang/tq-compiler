using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;
using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Abstract.CodeProcess.Core.Language.SyntaxNodes.Base;

namespace Abstract.CodeProcess.Core.EvaluationData.IntermediateTree.Expressions;

public class IrDotnetInvoke(
    SyntaxNode origin,
    IrExpression target,
    TypeReference[] generics,
    IrExpression[] args) : IrExpression(origin)
{
    public override TypeReference Type => ((FunctionTypeReference?)Target.Type)?.Returns ?? null!;

    public IrExpression Target { get; set; } = target;
    public TypeReference[] Generics { get; set; } = generics;
    public IrExpression[] Arguments { get; set; } = args;

    public override string ToString() => Generics.Length == 0
        ? $"dotnet_call {Target} ({string.Join(", ", Arguments.Select(e => e?.ToString() ?? "<nil>"))})"
        : $"dotnet_call {Target}<{string.Join(", ", Generics)}>({string.Join(", ", Arguments.Select(e => e?.ToString() ?? "<nil>"))})";
}
