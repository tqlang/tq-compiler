using System.Collections.Immutable;
using Tqc.Core;
using Tqc.IR.Nodes;

namespace Tqc.IR.ProgramMembers.Tq;

/// <summary>`typedef Name { case ... }` or `typedef(T) Name { case ... }`</summary>
public class TqTypedef(string name, SourceLocation location) : IType, ITqMember, IObjectType
{
    public IReadOnlyList<TqAttribute> Attributes { get; set; } = [];
    public MemberAccess Access { get; set; } = MemberAccess.Default;
    public SourceLocation SourceLocation => location;
    public string Name => name;

    /// <summary>The type the entries are stored as, as written; null if none was given.</summary>
    public IrExpression? UnderlyingType { get; set; }

    /// <summary>Named entries (TqNamedCase), functions and fields by name.</summary>
    public ImmutableDictionary<string, IMember> Members { get; init; } = ImmutableDictionary<string, IMember>.Empty;

    /// <summary>Entries made of numbers: `case 1, 2, 3`.</summary>
    public ImmutableArray<TqNumericCase> NumericCases { get; init; } = [];

    // Nothing can extend, be abstract or be an interface in a typedef, and its values are copied.
    public bool IsAbstract => false;
    public bool IsInterface => false;
    public bool IsFinal => true;
    public bool IsReference => false;

    public IMember? TryGetMember(string memberName) => Members.GetValueOrDefault(memberName);
    public IEnumerable<IMember> OwnMembers => Members.Values;

    public override string ToString() => $"typedef {Name}";
}

/// <summary>`case 1, 2, 3`: values the typedef accepts, with no name of their own.</summary>
public sealed record TqNumericCase(ImmutableArray<IrComptimeInt> Values, SourceLocation Location);

/// <summary>`case name` or `case name = value`: a value of the typedef that is reached by its name.</summary>
public class TqNamedCase(string name, IrExpression? value, SourceLocation location) : IField, ITqMember
{
    public IReadOnlyList<TqAttribute> Attributes { get; set; } = [];
    public MemberAccess Access { get; set; } = MemberAccess.Default;
    public SourceLocation SourceLocation => location;
    public string Name => name;
    public IrExpression? Value { get; set; } = value;

    /// <summary>The typedef it belongs to.</summary>
    public TqTypedef? Typedef { get; set; }

    public override string ToString() => $"case {Name}";
}
