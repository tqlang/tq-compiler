using System.Numerics;
using Tqc.Core;
using Tqc.IR.ProgramMembers;
using Tqc.IR.References;

namespace Tqc.IR.Nodes;

public abstract record IrNode(SourceLocation location);

public record IrBody(SourceLocation location) : IrStatement(location)
{
    public List<IrNode> Children = [];
}

#region Misc
public record IrArgumentsList(SourceLocation location, IrExpression[] arguments) : IrNode(location);
#endregion

#region Statement
public abstract record IrStatement(SourceLocation Location) : IrNode(Location);
public record IrReturnStatement(SourceLocation Location, IrExpression? ReturnValue) : IrStatement(Location);
public record IrExpressionStatement(SourceLocation Location, IrExpression Expression) : IrStatement(Location);

/// <summary>`if c { } elif d { } else { }` is an `IrIf` whose `Else` is the next `IrIf`.</summary>
public record IrIf(SourceLocation Location, IrExpression Condition, IrStatement Then, IrStatement? Else) : IrStatement(Location);

/// <summary>
/// `while`: the declaration runs once, the condition is tested before each iteration and the step runs after
/// each one. The declaration and the step are optional.
/// </summary>
public record IrWhile(
    SourceLocation Location,
    IrExpression? Declaration,
    IrExpression Condition,
    IrExpression? Step,
    IrStatement Body
) : IrStatement(Location);

/// <summary>`break`: leaves the closest loop.</summary>
public record IrBreak(SourceLocation Location) : IrStatement(Location);
#endregion

#region Expressions
public abstract record IrExpression(SourceLocation Location) : IrNode(Location);
public record IrAccess(SourceLocation Location, IrExpression baseExpr, IrReference access) : IrExpression(Location);
public record IrCall(SourceLocation Location, IrExpression callee, IrArgumentsList ArgumentsList) : IrExpression(Location);
/// <summary>`new T(args)`. `Constructor` is the constructor chosen by the type checker; null while it is not known.</summary>
public record IrNew(SourceLocation Location, IrExpression Type, IrArgumentsList ArgumentsList, IFunction? Constructor = null)
    : IrExpression(Location);
public record IrConvert(SourceLocation Location, IrExpression Expression, IrExpression toType) : IrExpression(Location);
public record IrAssign(SourceLocation Location, IrExpression Target, IrExpression Source) : IrExpression(Location);

public record IrSliceTypeModifier(SourceLocation Location, IrExpression subtype) : IrExpression(Location);

public record IrBinary(SourceLocation Location, BinaryOperator Operator, IrExpression Left, IrExpression Right) : IrExpression(Location);
public record IrUnary(SourceLocation Location, UnaryOperator Operator, IrExpression Operand, bool IsPostfix = false) : IrExpression(Location);
public record IrIndex(SourceLocation Location, IrExpression Target, IrExpression[] Indices) : IrExpression(Location);

/// <summary>`*T`: the type of a pointer to `T`.</summary>

/// <summary>`[a, b, c]`</summary>
public record IrArray(SourceLocation Location, IrExpression[] Elements) : IrExpression(Location);
#endregion

#region Values
public abstract record IrValue(SourceLocation Location) : IrExpression(Location);
public record IrReference(SourceLocation Location, Ref Reference) : IrValue(Location);
public record IrIdentifier(SourceLocation location, string identifier) : IrValue(location);
public record IrComptimeInt(SourceLocation Location, decimal BigInteger) : IrValue(Location);
public record IrComptimeDecimal(SourceLocation Location, decimal Value) : IrValue(Location);
public record IrComptimeBool(SourceLocation Location, bool Value) : IrValue(Location);

public record IrLocal(SourceLocation Location, IrExpression? type, string name) : IrValue(Location); 

public sealed record IrStringConstant(SourceLocation Location, string Value) : IrExpression(Location);
public sealed record IrStringConcat(SourceLocation Location, IrExpression[] Parts) : IrExpression(Location);

#endregion

