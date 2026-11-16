using System.Collections.Immutable;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.Semantics;

/// <summary>
/// Where the code that reaches for a member is.
/// `Namespace` is its namespace from the module name (e.g. `Std.Console`); `Scopes` holds the members that
/// contain the code, outermost first: every namespace of the path and, inside a struct or typedef, that type.
/// </summary>
internal readonly record struct AccessContext(string Namespace, ImmutableArray<IMember> Scopes)
{
    public string Module => ModuleOf(Namespace);

    public static AccessContext Of(IReadOnlyList<TqNamespace> path) =>
        new(string.Join('.', path.Select(node => node.Name)), [..path]);

    /// <summary>The same place, now inside `container`.</summary>
    public AccessContext Inside(IMember container) =>
        this with { Scopes = (Scopes.IsDefault ? [] : Scopes).Add(container) };

    public static string ModuleOf(string @namespace)
    {
        var dot = @namespace.IndexOf('.');
        return dot < 0 ? @namespace : @namespace[..dot];
    }
}
