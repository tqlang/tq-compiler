using Tqc.Core;
using Tqc.IR.Targets;

namespace Tqc.IR.Nodes;

/// <summary>What the code is when the sizes that depend on the target are these.</summary>
public sealed record IrSizeVariant(TargetSizes Sizes, IrExpression Expression);

/// <summary>What the statement is when the sizes that depend on the target are these.</summary>
public sealed record IrSizeStatementVariant(TargetSizes Sizes, IrStatement Statement);

/// <summary>
/// A decision made at compile time: the expression is not the same for every size `int` and `iptr` can have in the target, so
/// there is one version of it for each combination of candidate sizes, and the backend takes the one of the sizes it knows (or decides
/// when the program runs, if it cannot). It is only made when the versions differ: an expression that does not depend on the sizes is
/// the expression itself. Every version has the same type.
/// </summary>
public sealed record IrSizeChoice(SourceLocation Location, IrSizeVariant[] Variants) : IrExpression(Location);

/// <summary>The same as <see cref="IrSizeChoice"/> for a statement (a `return`, where the conversion of the value is part of it).</summary>
public sealed record IrSizeChoiceStatement(SourceLocation Location, IrSizeStatementVariant[] Variants) : IrStatement(Location);
