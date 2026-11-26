namespace Tqc.IR.Targets;

/// <summary>
/// One choice of the sizes a decision depends on: the bits `int/uint` have and the bits `iptr/uptr` have. A size that is null is not
/// part of the decision (the target has only one candidate for it, or the code does not depend on it).
/// </summary>
public readonly record struct TargetSizes(int? NaturalBits, int? NativeBits)
{
    /// <summary>Whether this is the choice a target with these sizes makes: every size the decision has is the one of the target.</summary>
    public bool Matches(TargetAbi abi) =>
        (NaturalBits == null || NaturalBits == abi.Natural) && (NativeBits == null || NativeBits == abi.Native);

    /// <summary>For the messages: `'int' has 32 bits and 'iptr' has 64 bits`.</summary>
    public string Describe()
    {
        var parts = new List<string>();
        if (NaturalBits is { } natural) parts.Add($"'int' has {natural} bits");
        if (NativeBits is { } native) parts.Add($"'iptr' has {native} bits");
        return string.Join(" and ", parts);
    }
}
