using System.Collections.Immutable;
using System.Globalization;
using Tqc.IR.Nodes;

namespace Tqc.IR.ProgramMembers;

/// <summary>
/// An entry of the GenericInstanceTable: a generic struct applied to arguments, e.g. `Integer(true, 32)`.
/// It is referenced like any struct. The arguments are kept as written; the concrete type is created later.
/// </summary>
public sealed class GenericInstance(IStruct definition, ImmutableArray<IrExpression> arguments) : IStruct
{
    public IStruct Definition => definition;
    public ImmutableArray<IrExpression> Arguments => arguments;

    // An instance is a concrete type: it has no parameters left.
    public string Name => $"{definition.Name}({string.Join(", ", arguments.Select(Describe))})";

    private static string Describe(IrExpression argument) => argument switch
    {
        IrComptimeInt integer => integer.BigInteger.ToString(CultureInfo.InvariantCulture),
        IrComptimeBool boolean => boolean.Value ? "true" : "false",
        IrStringConstant text => $"\"{text.Value}\"",
        IrReference reference => reference.Reference.ToString() ?? "?",
        _ => "?",
    };

    public override string ToString() => Name;
}
