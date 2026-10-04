using System.ComponentModel.Design;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.Semantics;

internal static class ReferenceBinder
{
    public static void BindReferences(TqModule module, IModule[] dependencies)
    {
        var context = new ScopeContext();
        context.PushFrame();

        context.Add(module.RootNamespace);
        foreach (var i in dependencies) context.Add(i.RootNamespace);
        
        ExploreNamespace(context, module.RootNamespace);
        
        context.PopFrame();
    }

    private static void ExploreNamespace(ScopeContext ctx, TqNamespace namespaceNode)
    {
        foreach (var fs in namespaceNode.FileScopes) ExploreFileScope(ctx, fs);
        foreach (var (_, ns) in namespaceNode.Namespaces) ExploreNamespace(ctx, ns);
    }

    private static void ExploreFileScope(ScopeContext ctx, TqFileScope fileScope)
    {
        ctx.PushFrame();
        
        foreach (var i in fileScope.Imports) SearchImportedReferences(ctx, i);
        foreach (var (_, i) in fileScope.Members) ExploreMember(ctx, i);
        
        ctx.PopFrame();
    }

    private static void SearchImportedReferences(ScopeContext ctx, TqImportMember importMember)
    {
        var namespaceReference = importMember.NamespaceReference;
        
        var member = ctx.TryGet(namespaceReference[0]);
        if (member == null) throw new NotImplementedException($"No module named '{namespaceReference[0]}' ({importMember.Location})");
        if (member is not INamespace @namespaceMember) throw new NotImplementedException($"'{namespaceReference[0]}' is not a namespace");
        var referredNamespace = namespaceMember;

        foreach (var i in namespaceReference[1..])
        {
            referredNamespace = referredNamespace.TryGetNamespace(i)
                ?? throw new NotImplementedException(
                    $"No namespace named '{i}' "
                    + $"at '{referredNamespace.Name}' ({importMember.Location})"
                );
        }

        if (importMember.Imports == null)
        {
            ctx.Add(referredNamespace.GetAllMembers());
            return;
        }

        foreach (var (alias, (_, reference)) in importMember.Imports)
        {
            var refe = referredNamespace.TryGetMember(reference);
            if (refe == null) throw new NotImplementedException($"'{reference}' is not a member of '{string.Join('.', namespaceReference)}'");
            ctx.AddWithAlias(refe, alias);
        }
    }

    private static void ExploreMember(ScopeContext ctx, IMember member)
    {
        switch (member)
        {
            case TqField field: ResolveField(ctx, field); break;
            case TqStruct @struct: ResolveStruct(ctx, @struct); break;
            case TqFunctionGroup @fng: ResolveFunctionGroup(ctx, @fng); break;
            case TqFunction @func: ResolveFunction(ctx, @func); break;
            default: throw new NotImplementedException();
        }
    }

    private static void ResolveField(ScopeContext ctx, TqField tqStruct)
    {
        //throw new NotImplementedException();
    }
    private static void ResolveStruct(ScopeContext ctx, TqStruct tqStruct)
    {
        //throw new NotImplementedException();
    }
    private static void ResolveFunctionGroup(ScopeContext ctx, TqFunctionGroup functionGroup)
    {
        foreach (var i in functionGroup) ResolveFunction(ctx, i);
    }
    private static void ResolveFunction(ScopeContext ctx, TqFunction function)
    {
        
    }
    

    private class ScopeContext
    {
        private List<(string name, IMember member)> references = [];
        private Stack<int> frames = [];

        public void Add(IMember member) => references.Add((member.Name, member));
        public void Add(params IMember[] member)
        {
            foreach (var i in member) references.Add((i.Name, i));
        }
        public void AddWithAlias(IMember member, string alias) => references.Add((alias, member));

        public IMember? TryGet(string key)
        {
            for (var i = references.Count - 1; i >= 0; i--)
                if (references[i].name == key) return references[i].member;
            return null;
        }
        
        public void PushFrame() => frames.Push(references.Count);
        public void PopFrame()
        {
            if (frames.Count == 0) throw new InvalidOperationException("No frame to pop");
            var start = frames.Pop();
            references.RemoveRange(start, references.Count - start);
        }
    }
}
