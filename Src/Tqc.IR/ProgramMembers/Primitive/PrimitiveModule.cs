namespace Tqc.IR.ProgramMembers.Primitive;

public class PrimitiveModule(INamespace rootNamespace) : IModule
{
    public INamespace RootNamespace => rootNamespace;
}
