using System.Collections.Immutable;

namespace Tqc.IR.Targets;

/// <summary>
/// What a target says about the size of the types whose size is not written in the language: what the backend of the target answers
/// when it is asked. A target can have more than one possible size for a type (`iptr` of .NET is 32 or 64 bits, depending on where it
/// runs): they are the candidates, and a decision that depends on the size has to hold for all of them, or be made once for each.
/// </summary>
/// <param name="ByteBits">The bits of a byte.</param>
/// <param name="NaturalBits">The candidate sizes of `int` and `uint`; none if they are not known.</param>
/// <param name="NativeBits">The candidate sizes of `iptr` and `uptr`; none if they are not known.</param>
public sealed record TargetAbi(int ByteBits, ImmutableArray<int> NaturalBits, ImmutableArray<int> NativeBits)
{
    /// <summary>A target that tells nothing: `int` and `uint` are not related to the integers with a size.</summary>
    public static TargetAbi Unknown { get; } = new(8, [], []);

    /// <summary>The size of `int` and `uint` when there is only one candidate.</summary>
    public int? Natural => NaturalBits is [var only] ? only : null;

    /// <summary>The size of `iptr` and `uptr` when there is only one candidate.</summary>
    public int? Native => NativeBits is [var only] ? only : null;
}
