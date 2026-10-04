using Tqc.IR.Nodes;

namespace Tqc.IR.ProgramMembers.Tq;

public record TqParameter(
    bool lazy,
    IrExpression type,
    string name
)
{
    public TqParameter(
        IrExpression type,
        string name,
        bool lazy = false
    ) : this(lazy, type, name) {}
}