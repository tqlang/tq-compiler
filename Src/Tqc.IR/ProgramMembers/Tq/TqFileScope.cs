namespace Tqc.IR.ProgramMembers.Tq;

public record TqFileScope(string FilePath)
{
    public TqImportMember[] Imports { get; init; }
    public Dictionary<string, IMember> Members { get; init; }
    
    public override string ToString() => $"fileScope {Path.GetFileName(FilePath)}";
}
