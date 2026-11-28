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
/// <summary>
/// `return`. `Cleanup` is what the type checker adds: the destruction of the variables that go out of scope, which runs after the value is
/// calculated and before the function returns.
/// </summary>
public record IrReturnStatement(SourceLocation Location, IrExpression? ReturnValue, IrStatement[]? Cleanup = null) : IrStatement(Location);
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

/// <summary>
/// `for item, index in collection`: runs the body once for each element, in order. It stays like this down to the backend,
/// as each target has its own way to iterate. The collection is evaluated once; `Item` and `Index` are read-only and get
/// their values before each turn.
/// </summary>
public record IrFor(
    SourceLocation Location,
    IrExpression Collection,
    IrLocal Item,
    IrLocal? Index,
    IrStatement Body
) : IrStatement(Location);

/// <summary>`break`: leaves the closest loop.</summary>
public record IrBreak(SourceLocation Location, IrStatement[]? Cleanup = null) : IrStatement(Location);

/// <summary>
/// The destruction of a variable that the type checker adds where the variable goes out of scope (the end of its block, `return`, `break`):
/// what `delete` does, but only if the variable was not deleted already.
/// </summary>
public record IrDestroy(SourceLocation Location, IrExpression Variable) : IrStatement(Location);

/// <summary>`delete value`: calls the destructor of the value (and of its fields). It is the only way a destructor is called.</summary>
public record IrDelete(SourceLocation Location, IrExpression Operand) : IrStatement(Location);
#endregion

#region Expressions
public abstract record IrExpression(SourceLocation Location) : IrNode(Location);
public record IrAccess(SourceLocation Location, IrExpression baseExpr, IrReference access) : IrExpression(Location);
public record IrCall(SourceLocation Location, IrExpression callee, IrArgumentsList ArgumentsList) : IrExpression(Location);
/// <summary>`new T(args)`. `Constructor` is the constructor chosen by the type checker; null while it is not known.</summary>
public record IrNew(SourceLocation Location, IrExpression Type, IrArgumentsList ArgumentsList, IFunction? Constructor = null)
    : IrExpression(Location);
public record IrConvert(SourceLocation Location, IrExpression Expression, IrExpression toType) : IrExpression(Location);
/// <summary>`target = source`. `DestroyOld` is what the type checker sets when the target is a variable that has a destructor: the value it has is destroyed before the new one replaces it.</summary>
public record IrAssign(SourceLocation Location, IrExpression Target, IrExpression Source, bool DestroyOld = false) : IrExpression(Location);

/// <summary>
/// A function of Tq as the delegate of .NET that is expected (`DelegateType` is `*Action`): `ldftn` of the function and the
/// constructor of the delegate. The type checker makes it from the name of a static function.
/// </summary>
public record IrFunctionDelegate(SourceLocation Location, IFunction Function, ITypeRef DelegateType) : IrExpression(Location);

/// <summary>
/// `d += f` and `d -= f` of a variable of a delegate: the delegate `d` with `f` added (`Delegate.Combine`) or removed
/// (`Delegate.Remove`), as the type of `d`. `Handler` is a delegate already.
/// </summary>
public record IrDelegateChain(SourceLocation Location, IrExpression Current, IrExpression Handler, bool Remove, ITypeRef DelegateType)
    : IrExpression(Location);

public record IrSliceTypeModifier(SourceLocation Location, IrExpression subtype) : IrExpression(Location);

/// <summary>`[5]T`: `Length` is the expression between the brackets (an integer literal).</summary>
public record IrArrayTypeModifier(SourceLocation Location, IrExpression Length, IrExpression subtype) : IrExpression(Location);

/// <summary>`a .. b` in an index: the elements from `a` to `b`, both included.</summary>
public record IrRange(SourceLocation Location, IrExpression From, IrExpression To) : IrExpression(Location);

/// <summary>
/// An arm of `match`: no patterns is `default`. `Body` is an expression or an `IrBody`. The type checker fills `Conditions`: for each
/// pattern, the `bool` expression that says whether the value of the match is equal to it.
/// </summary>
public record IrMatchArm(SourceLocation Location, IrExpression[] Patterns, IrNode Body, IrExpression[]? Conditions = null, IrLocal? Binding = null)
{
    public bool IsDefault => Patterns.Length == 0;
}

/// <summary>
/// `match subject { case a, b => x  default => y }`. The type checker gives `SubjectLocal`, a hidden variable that holds the
/// value of `Subject`, which the conditions of the arms compare.
/// </summary>
public record IrMatch(SourceLocation Location, IrExpression Subject, IrMatchArm[] Arms, IrLocal? SubjectLocal = null) : IrExpression(Location);

/// <summary>
/// A pattern of `match` that is a type: whether `Value` is a `Type`. When `Value` is itself a type (`SameType`: the subject of the
/// match is a `Type`, as the parameter of a generic function), whether it is that very type.
/// </summary>
public record IrTypeTest(SourceLocation Location, IrExpression Value, IrExpression Type, bool SameType = false) : IrExpression(Location);

/// <summary>`&x`: the slice that refers to the array `x`.</summary>
public record IrAddressOf(SourceLocation Location, IrExpression Operand) : IrExpression(Location);

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
public record IrComptimeInt(SourceLocation Location, BigInteger BigInteger) : IrValue(Location);
public record IrComptimeDecimal(SourceLocation Location, decimal Value) : IrValue(Location);
public record IrComptimeBool(SourceLocation Location, bool Value) : IrValue(Location);

/// <summary>`null`: nothing, for a `?T`.</summary>
public record IrNull(SourceLocation Location) : IrValue(Location);

public record IrLocal(SourceLocation Location, IrExpression? type, string name) : IrValue(Location)
{
    /// <summary>A variable the code cannot write, as the ones a `for` declares: it does not own the value it holds.</summary>
    public bool IsReadOnly { get; init; }
} 

public sealed record IrStringConstant(SourceLocation Location, string Value) : IrExpression(Location);
public sealed record IrStringConcat(SourceLocation Location, IrExpression[] Parts) : IrExpression(Location);

#endregion

