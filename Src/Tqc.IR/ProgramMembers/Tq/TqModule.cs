using System.Runtime.CompilerServices;

namespace Tqc.IR.ProgramMembers.Tq;

public record TqModule : IModule
{
    public TqNamespace RootNamespace { get; init; }

    /// <summary>Filled by the TypeChecker.</summary>
    public TypeTable Types { get; } = new();
    INamespace IModule.RootNamespace => RootNamespace;
}
