using Tqc.Diagnostics;
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
            RootNamespace = TreeLower.ParseNamespace(container.Root with { Name = container.name }),
        };
        AccessAssigner.Assign(module.RootNamespace);
        return module;
    }
    
    /// <summary>
    /// Types the expressions of a module whose names are bound (the result goes to `TqModule.Types`)
    /// and resolves the calls to overload groups. Errors go to `diagnostics`.
    /// </summary>
    public static void TypeCheck(IModule module, IModule[] dependencies, DiagnosticBag diagnostics)
    {
        if (module is not TqModule @tqModule) throw new ArgumentException("module is not TqModule", nameof(module));
        TypeChecker.Check(tqModule, dependencies, diagnostics);
    }

    /// <summary>The IR of a module as text, with the type of each expression (see <see cref="IrDumper"/>). For debugging.</summary>
    public static string DumpIr(IModule module, string name)
    {
        if (module is not TqModule @tqModule) throw new ArgumentException("module is not TqModule", nameof(module));
        return IrDumper.Dump(tqModule, name);
    }

    /// <summary>
    /// The `Std` module of the project merged with the one the compiler creates.
    /// Conflicting names are reported to `diagnostics`.
    /// </summary>
    public static IModule MergeStd(IModule userStd, DiagnosticBag diagnostics) =>
        StdMerger.Merge(userStd, diagnostics);

    /// <summary>
    /// Replaces unresolved names in the IR with concrete references. Errors go to `diagnostics`.
    /// `view` is what the module exposes to others (the module itself, except for a merged Std).
    /// </summary>
    public static void BindReferences(IModule module, IModule view, IModule[] dependencies, DiagnosticBag diagnostics)
    {
        if (module is not TqModule @tqModule) throw new ArgumentException("module is not TqModule", nameof(module));
        ReferenceBinder.BindReferences(tqModule, view, dependencies, diagnostics);
    }
    
}
