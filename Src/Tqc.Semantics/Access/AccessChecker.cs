using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// The access rules:
///   - public: any code; internal: code of the same module; private (default): code of the same namespace,
///     in any of its files, but not of the namespaces nested in it.
///   - `@allowAcessTo(x)` also lets in the code inside `x`, a namespace or a struct, and inside what is nested in it.
///   - `@denyAcessTo(x)` keeps that code out, whatever else is allowed.
/// Only members written in Tq have rules; the others (.NET, created by the compiler) are open.
/// </summary>
internal static class AccessChecker
{
    public static bool CanAccess(IMember? member, AccessContext from) => member switch
    {
        TqFunctionGroup group => group.Any(function => CanAccess(function, from)),
        ITqMember tqMember => CanAccess(tqMember.Access, from),
        _ => true,
    };

    public static bool CanAccess(MemberAccess access, AccessContext from)
    {
        if (Matches(access.Denied, from)) return false;

        var allowed = access.Visibility switch
        {
            Visibility.Public => true,
            Visibility.Internal => AccessContext.ModuleOf(access.Namespace) == from.Module,
            _ => access.Namespace == from.Namespace,
        };

        return allowed || Matches(access.Allowed, from);
    }

    /// <summary>The member a reference points to, if it points to one.</summary>
    public static IMember? MemberOf(Ref reference) => reference switch
    {
        FieldReference field => field.Field,
        PropertyReference property => property.Property,
        TypeMetaReference type => type.Type,
        FunctionTypeReference function => function.Function,
        FunctionGroupTypeReference group => group.FunctionGroup,
        _ => null,
    };

    /// <summary>Why `member` cannot be reached from `from`.</summary>
    public static string Explain(string name, IMember member, AccessContext from)
    {
        var access = member switch
        {
            TqFunctionGroup group => group.FirstOrDefault()?.Access,
            ITqMember tqMember => tqMember.Access,
            _ => null,
        } ?? MemberAccess.Default;

        if (Matches(access.Denied, from))
            return $"Access to '{name}' is denied to the code of '{from.Namespace}'";

        return access.Visibility == Visibility.Internal
            ? $"'{name}' is internal to the module '{AccessContext.ModuleOf(access.Namespace)}'"
            : $"'{name}' is private to the namespace '{access.Namespace}'";
    }

    /// <summary>Whether the code is inside one of the members; namespaces nested in it count as inside.</summary>
    private static bool Matches(IEnumerable<IMember> entries, AccessContext from)
    {
        if (from.Scopes.IsDefaultOrEmpty) return false;
        return entries.Any(entry => from.Scopes.Any(scope => ReferenceEquals(Canonical(scope), Canonical(entry))));
    }

    /// <summary>A merged namespace stands for the one of the project it came from.</summary>
    public static IMember Canonical(IMember member) =>
        member is PrimitiveNamespace { Original: { } original } ? Canonical(original) : member;
}
