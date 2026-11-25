using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal sealed partial class TypeChecker
{
    /// <summary>The delegate of .NET that a type is a reference to (`*Action`); null for any other type.</summary>
    private static DotnetDelegate? DelegateOf(ITypeRef? type) =>
        type != null && CompilerStd.Types.IsRef(type, out var referred) && referred is StructTypeReference { Struct: DotnetDelegate found }
            ? found
            : null;

    /// <summary>The functions of Tq that a name denotes (a single one is a group of one); null if it is not the name of a function.</summary>
    private static List<TqFunction>? FunctionsNamed(IrExpression expression)
    {
        var reference = expression switch
        {
            IrReference named => named.Reference,
            IrAccess { baseExpr: var owner, access: var member } when IsTypeOrNamespace(owner) => member.Reference,
            _ => null,
        };

        return reference switch
        {
            FunctionGroupTypeReference { FunctionGroup: TqFunctionGroup group } => [..group],
            FunctionTypeReference { Function: TqFunction function } => [function],
            _ => null,
        };
    }

    /// <summary>
    /// The delegate that the name of a function is where `delegateType` is expected: the static function whose parameters and return
    /// type are the ones of the `Invoke` of the delegate. Null, after reporting, if there is none.
    /// </summary>
    private IrExpression? FunctionAsDelegate(IrExpression written, ITypeRef delegateType, DotnetDelegate delegateInfo, string where)
    {
        var functions = FunctionsNamed(written)!;
        var name = functions.Count > 0 ? functions[0].Name : "?";
        var delegateName = TypeFormatter.Format(delegateType);

        if (delegateInfo.Invoke is not { } invoke || FunctionSignature.Of(invoke) is not { } wanted)
        {
            Error($"The delegate '{delegateName}' has no 'Invoke' that tells what a function has to look like to be it ({where})", written.Location);
            return null;
        }

        // Only a function that needs no value to run: a function of a namespace, or a `@static` one.
        var statics = functions.Where(function => function.Owner == null || function.IsStatic).ToList();
        if (statics.Count == 0)
        {
            Error($"'{name}' is not a static function, so it cannot become a '{delegateName}' ({where})", written.Location);
            return null;
        }

        var fitting = statics.Where(function => FunctionSignature.Of(function) is { } signature && signature.IsSameAs(wanted)).ToList();
        if (fitting.Count == 0)
        {
            var available = string.Join("; ", statics.Select(function => FunctionSignature.Of(function)?.ToString() ?? "?"));
            Error($"No overload of '{name}' has the signature of '{delegateName}' ({where}): it needs {wanted}, and '{name}' is {available}", written.Location);
            return null;
        }

        if (fitting.Count > 1)
        {
            Error($"'{name}' has {fitting.Count} overloads of the signature of '{delegateName}' ({where}), so it is not clear which one to use", written.Location);
            return null;
        }

        var result = new IrFunctionDelegate(written.Location, fitting[0], delegateType);
        types.Set(result, delegateType);
        return result;
    }

    /// <summary>What goes to a delegate: the name of a function becomes one, any other value has to be one already.</summary>
    private IrExpression? HandlerAs(IrExpression written, ITypeRef delegateType, DotnetDelegate delegateInfo, string where)
    {
        if (FunctionsNamed(written) != null) return FunctionAsDelegate(written, delegateType, delegateInfo, where);

        var value = Visit(written);
        var result = Coerce(value, delegateType, null, where);
        return TypeOf(result) != null ? result : null;
    }

    /// <summary>
    /// `x.Event += f` and `x.Event -= f` call the `add` and `remove` of the event; `d += f` and `d -= f` of a variable of a delegate
    /// add and remove from the delegate. `a += b` is lowered as `a = a + b` with the location of the whole assignment, which a
    /// hand-written `a = a + b` does not have. Null when it is neither: the assignment is checked as any other.
    /// </summary>
    private (IrExpression, ITypeRef?)? VisitDelegateAssign(IrAssign assign)
    {
        if (assign.Source is not IrBinary { Operator: BinaryOperator.Add or BinaryOperator.Sub } compound || compound.Location != assign.Location)
            return null;

        var remove = compound.Operator == BinaryOperator.Sub;

        switch (assign.Target)
        {
            // `d += f`: a variable, a parameter or a field named by itself.
            case IrReference { Reference: LocalReference or ParameterReference or FieldReference } named
                when DelegateOf(TypeOfReference(named.Reference)) != null:
                return ChainAssign(assign, Visit(named), compound, remove);

            // `x.Event += f`, or `x.field += f` of a delegate; only for an `x` that is a variable.
            case IrAccess { baseExpr: IrReference { Reference: LocalReference or ParameterReference or FieldReference or ThisReference } baseNamed, access.Reference: UnsolvedRef member } access:
            {
                var receiver = Visit(baseNamed);
                var found = ContainerOf(TypeOf(receiver))?.TryGetMember(member.Value);

                if (found is DotnetEvent @event) return SubscribeEvent(assign, access, receiver, @event, compound, remove);

                if (found is IField && Visit(access) is var target && DelegateOf(TypeOf(target)) != null)
                    return ChainAssign(assign, target, compound, remove);

                return null;
            }

            default:
                return null;
        }
    }

    private (IrExpression, ITypeRef?) ChainAssign(IrAssign assign, IrExpression target, IrBinary compound, bool remove)
    {
        var type = TypeOf(target)!;
        var handler = HandlerAs(compound.Right, type, DelegateOf(type)!, $"'{(remove ? "-=" : "+=")}'");
        if (handler == null) return (assign with { Target = target }, type);

        var chain = new IrDelegateChain(assign.Location, target, handler, remove, type);
        types.Set(chain, type);
        return (assign with { Target = target, Source = chain }, type);
    }

    private (IrExpression, ITypeRef?) SubscribeEvent(
        IrAssign assign, IrAccess access, IrExpression receiver, DotnetEvent @event, IrBinary compound, bool remove)
    {
        var symbol = remove ? "-=" : "+=";
        var failed = (assign with { Source = compound }, (ITypeRef?)null);

        if (@event.IsStatic)
        {
            Error($"'{@event.Name}' is a static event: only the events of a value can be used with '{symbol}' for now", access.Location);
            return failed;
        }

        var accessor = remove ? @event.Remove : @event.Add;
        if (accessor == null)
        {
            Error($"The event '{@event.Name}' cannot be {(remove ? "removed from" : "added to")}: it has no '{(remove ? "remove" : "add")}'", access.Location);
            return failed;
        }

        if (DelegateOf(@event.Type) is not { } delegateInfo)
        {
            Error($"The event '{@event.Name}' takes a delegate that is not supported yet: {TypeFormatter.Format(@event.Type)} (generic delegates are not imported)", access.Location);
            return failed;
        }

        var handler = HandlerAs(compound.Right, @event.Type, delegateInfo, $"'{symbol}' of the event '{@event.Name}'");
        if (handler == null) return failed;

        var callee = new IrAccess(assign.Location, receiver, new IrReference(assign.Location, new FunctionTypeReference(accessor)));
        return (new IrCall(assign.Location, callee, new IrArgumentsList(assign.Location, [handler])), null);
    }
}
