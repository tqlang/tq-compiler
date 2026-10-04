namespace Tqc.IR.ProgramMembers;

public interface INamespace : IMember
{
    public string Name { get; } 
    
    public INamespace? TryGetNamespace(string namespaceName);
    public IMember? TryGetMember(string memberName);
    public IMember[] GetAllMembers();
}
