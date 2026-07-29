using System.Text;
using Abstract.CodeProcess.Core;
using Tq.CodeProcess.Core.EvaluationData.IntermediateTree;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;

namespace Tq.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

public class GenericTypeImplReference(ITypeReference generic) : Reference, ITypeReference
{
    public readonly ITypeReference GenericType = generic;
    public readonly List<IrExpression> Arguments = [];

    public Alignment Length => 0;
    public Alignment Alignment => 0;
    public bool IsGeneric => false;
    public override ITypeReference Type => new TypeTypeReference(this);

    public override string ToString() => new StringBuilder()
        .Append($"{GenericType}")
        .Append('(').AppendJoin(", ", Arguments).Append(')')
        .ToString();

}
