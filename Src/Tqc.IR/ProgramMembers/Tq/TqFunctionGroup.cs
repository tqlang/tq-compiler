using System.Collections;

namespace Tqc.IR.ProgramMembers.Tq;

public class TqFunctionGroup(string name) : IFunctionGroup, IEnumerable<TqFunction>
{
    public string Name => name;
    public List<TqFunction> Functions = [];

    public IEnumerator<TqFunction> GetEnumerator() => Functions.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    public override string ToString() => $"funcGroup {Name}";
}
