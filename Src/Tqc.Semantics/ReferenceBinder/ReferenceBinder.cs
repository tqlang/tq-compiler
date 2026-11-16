using Tqc.Diagnostics;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// Replaces unresolved names (UnsolvedRef) in the IR with concrete references.
/// A name is looked up as a keyword type first (`i32`, `bool`...; see BuiltinTypes), then in these scopes, outermost to innermost:
///   1. module roots (this module and its direct dependencies)
///   2. wildcard imports (`from X import`); a conflict is only an error when the name is used
///   3. members of every enclosing namespace
///   4. named imports (`from X import { a, b as c }`)
/// </summary>
internal static partial class ReferenceBinder
{
    /// <summary>
    /// Binding runs twice over the module: the attributes first, because the members named by `@allowAcessTo`
    /// and `@denyAcessTo` decide who can reach what; then everything else.
    /// </summary>
    private enum Phase { Attributes, Bodies }

    public static void BindReferences(TqModule module, IModule view, IModule[] dependencies, DiagnosticBag diagnostics)
    {
        var context = new BindingContext(diagnostics);
        context.PushFrame();

        List<INamespace> roots = [view.RootNamespace, ..dependencies.Select(dependency => dependency.RootNamespace)];
        // The compiler's Std is always there, unless a Std module (already merged with it) is in scope.
        if (roots.All(root => root.Name != CompilerStd.ModuleName)) roots.Add(CompilerStd.Module.RootNamespace);

        foreach (var root in roots) context.Add(root);

        BindNamespace(context, module.RootNamespace, [], Phase.Attributes);
        BindNamespace(context, module.RootNamespace, [], Phase.Bodies);

        context.PopFrame();
    }

    private static void BindNamespace(BindingContext ctx, TqNamespace namespaceNode, List<TqNamespace> path, Phase phase)
    {
        path.Add(namespaceNode);

        foreach (var fileScope in namespaceNode.FileScopes)
        {
            ctx.Accessor = AccessContext.Of(path);
            BindFileScope(ctx, fileScope, path, phase);
        }

        foreach (var (_, child) in namespaceNode.Namespaces) BindNamespace(ctx, child, path, phase);

        path.RemoveAt(path.Count - 1);
    }

    private static void BindFileScope(BindingContext ctx, TqFileScope fileScope, List<TqNamespace> path, Phase phase)
    {
        // Imports only see the enclosing namespaces and module roots, so resolve them before pushing the file scopes.
        // They are resolved once: the second phase reuses the first one's result.
        if (!ctx.ImportCache.TryGetValue(fileScope, out var imports))
        {
            List<(string Name, Ref Reference)> star = [];
            List<(string Name, Ref Reference)> explicitList = [];
            foreach (var import in fileScope.Imports)
                ResolveImport(ctx, import, path, explicitList, star);

            imports = (explicitList, star);
            ctx.ImportCache[fileScope] = imports;
        }

        var (explicitImports, starImports) = imports;

        ctx.PushFrame();
        foreach (var (name, reference) in starImports) ctx.Add(name, reference);

        foreach (var namespaceNode in path)
        {
            ctx.PushFrame();
            foreach (var member in namespaceNode.GetAllMembers()) ctx.Add(member);
        }

        ctx.PushFrame();
        foreach (var (name, reference) in explicitImports) ctx.Add(name, reference);

        foreach (var member in fileScope.Members.Values)
        {
            if (phase == Phase.Attributes) BindMemberAttributes(ctx, member);
            else BindMember(ctx, member);
        }

        ctx.PopFrame();
        for (var i = 0; i < path.Count; i++) ctx.PopFrame();
        ctx.PopFrame();
    }
}
