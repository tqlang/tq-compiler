using Tqc.Core;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal sealed partial class TypeChecker
{
    /// <summary>
    /// Set by an assignment for the target it is about to visit: a property that is written is not read, so it
    /// is kept as it is for the assignment to call its setter. Whoever reads it takes it, so that what is
    /// inside the target (`a.b.p = 1`: `a.b`) is read as usual.
    /// </summary>
    private bool _targetNext;

    private bool TakeTarget()
    {
        var value = _targetNext;
        _targetNext = false;
        return value;
    }

    /// <summary>A name: a property of the struct itself is read, or written, through `this`.</summary>
    private (IrExpression, ITypeRef?) VisitReference(IrReference reference)
    {
        var asTarget = TakeTarget();
        if (reference.Reference is not PropertyReference { Property: var property })
            return (reference, TypeOfReference(reference.Reference));

        var type = PropertyTypeOf(property);
        return asTarget ? (reference, type) : ReadProperty(ImplicitThis(reference.Location, property), reference.Location, property, type);
    }

    /// <summary>`a.p`: the getter is called on `a`, unless it is the target of an assignment.</summary>
    private (IrExpression, ITypeRef?) PropertyAccess(IrAccess access, TqProperty property, bool asTarget)
    {
        var type = PropertyTypeOf(property);
        return asTarget ? (access, type) : ReadProperty(access.baseExpr, access.Location, property, type);
    }

    private static IrExpression ImplicitThis(SourceLocation location, TqProperty property) =>
        new IrReference(location, new ThisReference(property.Owner as IType
            ?? throw new InvalidOperationException($"The property '{property.Name}' has no struct")));

    private static TqProperty? PropertyOf(IrExpression target) => target switch
    {
        IrReference { Reference: PropertyReference reference } => reference.Property,
        IrAccess { access.Reference: PropertyReference reference } => reference.Property,
        _ => null,
    };

    private (IrExpression, ITypeRef?) ReadProperty(IrExpression receiver, SourceLocation location, TqProperty property, ITypeRef? type)
    {
        EnsureAccessors(property);
        if (property.Getter == null)
        {
            Error($"The property '{property.Name}' has no 'get'", location);
            return (receiver, null);
        }

        var callee = new IrAccess(location, receiver, new IrReference(location, new FunctionTypeReference(property.Getter)));
        return (new IrCall(location, callee, new IrArgumentsList(location, [])), type);
    }

    /// <summary>`a.p = v` calls the setter of `p` on `a` with the value, converted to the type of the property.</summary>
    private (IrExpression, ITypeRef?) WriteProperty(IrAssign assign, IrExpression target, IrExpression source, TqProperty property)
    {
        EnsureAccessors(property);
        var type = PropertyTypeOf(property);

        if (property.Setter == null)
        {
            Error($"The property '{property.Name}' has no 'set'", assign.Location);
            return (assign with { Target = target, Source = source }, type);
        }

        source = Coerce(source, type, property.Type, $"the property '{property.Name}'");

        var receiver = target is IrAccess access ? access.baseExpr : ImplicitThis(assign.Location, property);
        var callee = new IrAccess(assign.Location, receiver, new IrReference(assign.Location, new FunctionTypeReference(property.Setter)));
        return (new IrCall(assign.Location, callee, new IrArgumentsList(assign.Location, [source])), type);
    }

    /// <summary>
    /// The type declared by the property or, if it has none, the one of the functions that define it:
    /// what the getter returns or what the setter takes.
    /// </summary>
    private ITypeRef? PropertyTypeOf(TqProperty property)
    {
        EnsureAccessors(property);

        if (property.Type != null) return TypeFromExpression(property.Type);
        if (property.Getter != null && FunctionSignature.Of(property.Getter) is { } getter) return getter.Return;
        if (property.Setter != null && FunctionSignature.Of(property.Setter) is { Parameters.Count: 1 } setter) return setter.Parameters[0];
        return null;
    }

    /// <summary>
    /// Finds the functions named by `get = f` and `set = f`, once, as code of the struct. The function has to be
    /// an instance function with the signature of the accessor, and the types of both have to be the type of the
    /// property.
    /// </summary>
    private void EnsureAccessors(TqProperty property)
    {
        if (property.AccessorsResolved) return;
        property.AccessorsResolved = true;

        void Resolve()
        {
            if (property.GetterSource != null) property.Getter = ResolveAccessor(property, property.GetterSource, true);
            if (property.SetterSource != null) property.Setter = ResolveAccessor(property, property.SetterSource, false);

            // Without a declared type both accessors have to agree on the one they give.
            if (property.Type == null
                && property.Getter != null && FunctionSignature.Of(property.Getter)?.Return is { } read
                && property.Setter != null && FunctionSignature.Of(property.Setter) is { Parameters.Count: 1 } write
                && write.Parameters[0] is { } written && !read.Equals(written))
                Error($"The 'get' of '{property.Name}' gives '{TypeFormatter.Format(read)}' but its 'set' takes '{TypeFormatter.Format(written)}'",
                    property.SourceLocation);
        }

        if (property.Owner is IMember owner) InsideOf(owner, Resolve);
        else Resolve();
    }

    private IFunction? ResolveAccessor(TqProperty property, IrExpression source, bool isGetter)
    {
        var accessor = isGetter ? "get" : "set";
        var visited = Visit(source);
        var parameters = isGetter ? 0 : 1;

        IReadOnlyList<IFunction> candidates = CalleeReference(visited) switch
        {
            FunctionGroupTypeReference { FunctionGroup: TqFunctionGroup group } => [..group],
            FunctionTypeReference { Function: var named } => [named],
            _ => [],
        };

        if (candidates.Count == 0)
        {
            Error($"The '{accessor}' of the property '{property.Name}' has to be a function", source.Location);
            return null;
        }

        var matching = candidates
            .Where(candidate => FunctionSignature.Of(candidate)?.Parameters.Count == parameters
                                && candidate is not IMethod { IsStatic: true })
            .ToList();

        if (matching.Count != 1)
        {
            var wanted = isGetter ? "that takes nothing" : "that takes one value";
            Error(matching.Count == 0
                ? $"'{candidates[0].Name}' has no instance function {wanted}, which the '{accessor}' of '{property.Name}' needs"
                : $"More than one '{candidates[0].Name}' {wanted}: the '{accessor}' of '{property.Name}' is ambiguous", source.Location);
            return null;
        }

        var chosen = matching[0];
        var declared = property.Type != null ? TypeFromExpression(property.Type) : null;
        var signature = FunctionSignature.Of(chosen)!;
        var actual = isGetter ? signature.Return : signature.Parameters[0];

        if (declared != null && actual != null && !declared.Equals(actual))
            Error($"'{chosen.Name}' {(isGetter ? "returns" : "takes")} '{TypeFormatter.Format(actual)}', " +
                  $"but the property '{property.Name}' is '{TypeFormatter.Format(declared)}'", source.Location);

        return chosen;
    }
}
