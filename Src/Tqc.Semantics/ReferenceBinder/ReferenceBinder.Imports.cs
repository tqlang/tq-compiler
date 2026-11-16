using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal static partial class ReferenceBinder
{
    private static void ResolveImport(
        BindingContext ctx,
        TqImportMember import,
        List<TqNamespace> path,
        List<(string Name, Ref Reference)> explicitImports,
        List<(string Name, Ref Reference)> starImports)
    {
        var segments = import.NamespaceReference;
        if (segments.Length == 0) return;

        var current = ResolveImportRoot(ctx, segments[0], path);
        if (current == null)
        {
            ctx.Error($"'{segments[0]}' is not a module or a namespace", import.Location);
            return;
        }

        for (var i = 1; i < segments.Length; i++)
        {
            // .NET static classes also act as namespaces.
            var next = current.TryGetNamespace(segments[i]) ?? current.TryGetMember(segments[i]) as INamespace;
            if (next == null)
            {
                ctx.Error($"'{current.Name}' has no namespace named '{segments[i]}'", import.Location);
                return;
            }

            current = next;
        }

        if (import.Imports == null)
        {
            foreach (var member in current.GetAllMembers())
                starImports.Add((member.Name, member.GetReference()));
            return;
        }

        var qualifiedName = string.Join('.', segments);
        foreach (var (alias, (location, name)) in import.Imports)
        {
            var member = current.TryGetMember(name);
            if (member == null)
            {
                ctx.Error($"'{name}' is not a member of '{qualifiedName}'", location);
                continue;
            }

            explicitImports.Add((alias, member.GetReference()));
        }
    }

    /// <summary>First import segment: a relative namespace (innermost first) or a module name.</summary>
    private static INamespace? ResolveImportRoot(BindingContext ctx, string name, List<TqNamespace> path)
    {
        for (var i = path.Count - 1; i >= 0; i--)
        {
            var relative = path[i].TryGetNamespace(name);
            if (relative != null) return relative;
        }

        return ctx.TryGet(name, out _) is NamespaceReference { Namespace: var module } ? module : null;
    }
}
