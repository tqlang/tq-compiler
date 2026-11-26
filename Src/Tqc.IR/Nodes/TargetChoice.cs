using Tqc.Core;

namespace Tqc.IR.Nodes;

/// <summary>What the code is when `int` and `uint` have this many bits.</summary>
public sealed record IrNaturalVariant(int Bits, IrExpression Expression);

/// <summary>What the statement is when `int` and `uint` have this many bits.</summary>
public sealed record IrNaturalStatementVariant(int Bits, IrStatement Statement);

/// <summary>
/// A decision made at compile time: the expression is not the same for every size `int` and `uint` can have in the target, so
/// there is one version of it for each candidate size, and the backend takes the one of the size it knows (or decides when the
/// program runs, if it cannot). It is only made when the versions differ: an expression that does not depend on the size is
/// the expression itself. Every version has the same type.
/// </summary>
public sealed record IrNaturalChoice(SourceLocation Location, IrNaturalVariant[] Variants) : IrExpression(Location);

/// <summary>The same as <see cref="IrNaturalChoice"/> for a statement (a `return`, where the conversion of the value is part of it).</summary>
public sealed record IrNaturalChoiceStatement(SourceLocation Location, IrNaturalStatementVariant[] Variants) : IrStatement(Location);
