using System.Collections.Immutable;
using Tqc.Core;
using Tqc.IR.Nodes;

namespace Tqc.IR.ProgramMembers.Tq;

/// <summary>An attribute written above a member (`@public`, `@packed(8)`), with its arguments as written.</summary>
public abstract record TqAttribute(SourceLocation Location) : IAttribute;

/// <summary>An attribute of the language and the standard library; its arguments were already validated.</summary>
public sealed record TqBuiltinAttribute(
    BuiltinAttribute Kind,
    string Name,
    ImmutableArray<IrExpression> Arguments,
    SourceLocation Location) : TqAttribute(Location);

/// <summary>An attribute the compiler does not know. Attributes defined by the user are not implemented yet.</summary>
public sealed record TqUnknownAttribute(
    string Name,
    ImmutableArray<IrExpression> Arguments,
    SourceLocation Location) : TqAttribute(Location);
