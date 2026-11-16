using Tqc.IR.Nodes;

namespace Tqc.IR.ProgramMembers.Tq;

public record TqParameter
{
    public bool Lazy { get; init; }
    public IrExpression Type { get; set; }
    public string Name { get; init; }
    public IrExpression? DefaultValue { get; set; }

    public TqParameter(
        IrExpression type,
        string name,
        IrExpression? defaultValue = null,
        bool lazy = false
    )
    {
        Lazy = lazy;
        Type = type;
        Name = name;
        DefaultValue = defaultValue;
    }
}
