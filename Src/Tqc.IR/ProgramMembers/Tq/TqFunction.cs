using Tqc.Core;
using Tqc.IR.Nodes;

namespace Tqc.IR.ProgramMembers.Tq;

public class TqFunction(string name, TqParameter[] parameters, IrExpression? returnType, SourceLocation location) : IFunction
{
    public SourceLocation SourceLocation => location;
    public string Name => name;
    public TqParameter[] Parameters => parameters;
    public IrExpression? ReturnType => returnType;
    
    public IrBody? Body { get; set; }
    
    public override string ToString() => $"func {Name}";
}
