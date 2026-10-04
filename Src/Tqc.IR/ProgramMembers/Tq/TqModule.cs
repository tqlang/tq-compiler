using System.Runtime.CompilerServices;

namespace Tqc.IR.ProgramMembers.Tq;

public record TqModule : IModule
{
    public TqNamespace RootNamespace { get; init; }
    INamespace IModule.RootNamespace => RootNamespace;
}
