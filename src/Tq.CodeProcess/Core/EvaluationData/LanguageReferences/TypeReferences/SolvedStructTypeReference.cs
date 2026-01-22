using Abstract.CodeProcess.Core.EvaluationData.LanguageObjects;

namespace Abstract.CodeProcess.Core.EvaluationData.LanguageReferences.TypeReferences;

public class SolvedStructTypeReference(StructObject struc) : TypeReference
{
    public readonly StructObject Struct = struc;
    public override Alignment Length => Struct.Length ?? 0;
    public override Alignment Alignment => Struct.Alignment ?? 0;
    public override string ToString() => $"Struct<{string.Join('.', Struct.Global)}>";

    public int CalculateSuitability(SolvedStructTypeReference to)
    {
        if (Struct == to.Struct) return 3;
        // TODO check casting possibility
        return 0;
    }
}
