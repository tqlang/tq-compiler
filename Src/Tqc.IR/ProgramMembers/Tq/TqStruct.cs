using System.Collections.Immutable;
using Tqc.Core;
using Tqc.IR.Nodes;
using Tqc.IR.References;

namespace Tqc.IR.ProgramMembers.Tq;

public class TqStruct(string name, SourceLocation location) : IStruct, ITqMember, IObjectType
{
    public IReadOnlyList<TqAttribute> Attributes { get; set; } = [];
    public MemberAccess Access { get; set; } = MemberAccess.Default;
    public SourceLocation SourceLocation => location;
    public string Name => name;
    public bool IsInterface => Has(BuiltinAttribute.Interface);
    public bool IsAbstract => IsInterface || Has(BuiltinAttribute.Abstract);
    public bool IsFinal => Has(BuiltinAttribute.Final);
    public bool IsReference => Has(BuiltinAttribute.Class);

    private bool Has(BuiltinAttribute kind) =>
        Attributes.OfType<TqBuiltinAttribute>().Any(attribute => attribute.Kind == kind);

    public IReadOnlyList<TqParameter> Parameters { get; init; } = [];

    /// <summary>The struct it extends, as written (a reference once bound); null if it extends none.</summary>
    public IrExpression? Extends { get; set; }

    /// <summary>The protocols it implements (`implements A, B`), as written (references once bound).</summary>
    public IReadOnlyList<IrExpression> Implements { get; set; } = [];

    /// <summary>The protocols it implements, once bound; the ones that are not known are left out.</summary>
    public IEnumerable<IMemberContainer> Protocols => Implements.Select(Ancestry.ProtocolOf).OfType<IMemberContainer>();

    /// <summary>Fields and functions by name; overloads are grouped in a TqFunctionGroup.</summary>
    public ImmutableDictionary<string, IMember> Members { get; init; } = ImmutableDictionary<string, IMember>.Empty;

    public IReadOnlyList<TqConstructor> Constructors { get; init; } = [];
    public TqDestructor? Destructor { get; init; }

    /// <summary>
    /// The fields whose destructors run after the destructor of the struct (in the order they are declared; they are destroyed in reverse): the
    /// ones that are structs by value with a destructor. Null until the type checker finds them. A struct that has any has a destructor of its own
    /// even if it declares none.
    /// </summary>
    public IReadOnlyList<TqField>? DestroyedFields { get; set; }

    /// <summary>Whether the struct has a destructor of its own: the one it declares, or the one that destroys its fields.</summary>
    public bool DeclaresDestructor => Destructor != null || DestroyedFields is { Count: > 0 };

    public IEnumerable<IMember> OwnMembers => Members.Values;

    /// <summary>What it extends, once bound: a struct of Tq, of .NET or any other.</summary>
    public IMemberContainer? Base =>
        Extends is IrReference { Reference: TypeMetaReference { Type: IMemberContainer container } } ? container : null;

    /// <summary>The struct it extends, once bound, even if it does not list its members.</summary>
    public IStruct? BaseStruct =>
        Extends is IrReference { Reference: TypeMetaReference { Type: IStruct baseStruct } } ? baseStruct : null;

    /// <summary>A member of the struct or, failing that, of what it extends.</summary>
    public IMember? TryGetMember(string memberName)
    {
        HashSet<TqStruct> visited = [];

        for (IMemberContainer? current = this; current != null;)
        {
            // Anything else (a .NET class, for instance) knows its own ancestors.
            if (current is not TqStruct structure) return current.TryGetMember(memberName);
            if (!visited.Add(structure)) return null; // a cycle was reported when binding

            if (structure.Members.TryGetValue(memberName, out var member)) return member;
            current = structure.Base;
        }

        return null;
    }

    public override string ToString() => $"struct {Name}";
}
