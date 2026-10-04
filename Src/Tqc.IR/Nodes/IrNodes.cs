using System.Numerics;
using Tqc.Core;
using Tqc.IR.References;

namespace Tqc.IR.Nodes;

public abstract record IrNode(SourceLocation location);

public record IrBody(SourceLocation location) : IrNode(location)
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
#endregion

#region Expressions
public abstract record IrExpression(SourceLocation Location) : IrNode(Location);
public record IrAccess(SourceLocation Location, IrExpression baseExpr, IrReference access) : IrExpression(Location);
public record IrCall(SourceLocation Location, IrExpression callee, IrArgumentsList ArgumentsList) : IrExpression(Location);
public record IrNew(SourceLocation Location, IrExpression Type, IrArgumentsList ArgumentsList) : IrExpression(Location);
public record IrConvert(SourceLocation Location, IrExpression Expression, IrExpression toType) : IrExpression(Location);
public record IrAssign(SourceLocation Location, IrExpression Target, IrExpression Source) : IrExpression(Location);

public record IrSliceTypeModifier(SourceLocation Location, IrExpression subtype) : IrExpression(Location);
#endregion

#region Values
public abstract record IrValue(SourceLocation Location) : IrExpression(Location);
public record IrReference(SourceLocation Location, Ref Reference) : IrValue(Location);
public record IrIdentifier(SourceLocation location, string identifier) : IrValue(location);
public record IrComptimeInt(SourceLocation Location, decimal BigInteger) : IrValue(Location);

public record IrLocal(SourceLocation Location, IrExpression? type, string name) : IrValue(Location); 

public sealed record IrStringConstant(SourceLocation Location, string Value) : IrExpression(Location);
public sealed record IrStringConcat(SourceLocation Location, IrExpression[] Parts) : IrExpression(Location);

#endregion

