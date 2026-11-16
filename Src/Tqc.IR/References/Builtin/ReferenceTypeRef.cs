namespace Tqc.IR.References;

public record ReferenceTypeRef(ITypeRef Referred) : Ref, IBuiltinTypeRef;
