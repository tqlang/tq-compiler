using Tqc.IR.References;

namespace Tqc.IR.Nodes;

public static class IrVariables
{
    /// <summary>What identifies a variable: a local by its name and place, a parameter by itself. Null for what is not a variable.</summary>
    public static object? KeyOf(IrNode node) => node switch
    {
        IrReference { Reference: LocalReference local } => (local.Local.name, local.Local.Location),
        IrReference { Reference: ParameterReference parameter } => parameter.Parameter,
        IrLocal declared => (declared.name, declared.Location),
        _ => null,
    };
}
