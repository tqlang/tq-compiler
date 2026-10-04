namespace Tqc.IR.ProgramMembers.Tq;

public abstract record TqAttribute : IAttribute;
public abstract record TqBuiltinAttribute : TqAttribute;

public record TqUnknownAttribute : TqAttribute;

public record TqCallableAttribute : TqAttribute;
