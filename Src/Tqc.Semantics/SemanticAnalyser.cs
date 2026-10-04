using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.Syntax;

namespace Tqc.Semantics;

public static class SemanticAnalyser
{
    public static IModule Resolve(SyntaxModuleContainer container)
    {
        var module = new TqModule
        {
            RootNamespace = TreeLower.ParseNamespace(container.Root),
        };
        return module;
    }

    public static void BindReferences(IModule module, IModule[] dependencies)
    {
        if (module is not TqModule @tqModule) throw new ArgumentException("module is not TqModule", nameof(module));
        ReferenceBinder.BindReferences(tqModule, dependencies);
    }
    
}
