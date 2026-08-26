using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

public class TypedefReference(TypedefObject typedef) : Reference, ITypeReference
{
    public readonly TypedefObject Typedef = typedef;
    public Alignment Length => 0;
    public Alignment Alignment => 0;
    public bool IsGeneric => Typedef.Generic;
    public override ITypeReference Type => new TypeTypeReference(this);
    public override string ToString() => $"Typedef<{string.Join('.', Typedef.Global)}>";
}