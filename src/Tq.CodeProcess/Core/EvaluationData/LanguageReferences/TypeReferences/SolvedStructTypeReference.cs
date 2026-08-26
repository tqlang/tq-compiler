using Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences.Builtin;
using Tq.CodeProcess.Core.EvaluationData.LanguageObjects;
using Tq.CodeProcess.Core.EvaluationData.LanguageReferences;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

public class StructReference(StructObject struc) : Reference, ITypeReference
{
    public readonly StructObject Struct = struc;
    public bool IsGeneric => Struct.Generic;
    public override ITypeReference Type => new TypeTypeReference(this);
    
    public override string ToString() => $"Struct<{string.Join('.', Struct.Global)}>";

    public int CalculateSuitability(StructReference to)
    {
        if (Struct == to.Struct) return 3;
        // TODO check casting possibility
        return 0;
    }
}
