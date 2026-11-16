using Tqc.Core;
using Tqc.IR.Nodes;

namespace Tqc.IR.ProgramMembers.Tq;

public class TqField(
    IrExpression? type,
    string name,
    IrExpression? value,
    SourceLocation location
) : IField, ITqMember
{
    public IReadOnlyList<TqAttribute> Attributes { get; set; } = [];
    public MemberAccess Access { get; set; } = MemberAccess.Default;
    public SourceLocation SourceLocation => location;
    public string Name => name;

    public IrExpression? Type { get; set; } = type;
    public IrExpression? Value { get; set; } = value;

    public override string ToString() => $"field {Name}";
}
