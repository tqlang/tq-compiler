using Tqc.Core;

namespace Tqc.IR.ProgramMembers.Tq;

public class TqStruct(string name, SourceLocation location) : IStruct
{
    public SourceLocation SourceLocation => location;
    public string Name => name;
    
    public override string ToString() => $"struct {Name}";
}
