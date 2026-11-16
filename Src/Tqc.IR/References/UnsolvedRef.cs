namespace Tqc.IR.References;

/// <summary>A name not resolved yet. The ReferenceBinder replaces it with a concrete reference.</summary>
public record UnsolvedRef(string Value) : Ref;
