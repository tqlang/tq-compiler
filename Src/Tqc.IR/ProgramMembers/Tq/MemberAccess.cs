using System.Collections.Immutable;

namespace Tqc.IR.ProgramMembers.Tq;

/// <summary>
/// Who can reach a member. `Namespace` is where it is declared, from the module name, e.g. `Std.Console`.
/// `Allowed` and `Denied` hold the namespaces and structs named by `@allowAcessTo` and `@denyAcessTo`:
/// the code inside them, and inside what is nested in them, is affected.
/// </summary>
public sealed record MemberAccess(
    Visibility Visibility,
    ImmutableArray<IMember> Allowed,
    ImmutableArray<IMember> Denied,
    string Namespace)
{
    public static readonly MemberAccess Default = new(Visibility.Private, [], [], "");
}
