using System.Numerics;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

internal sealed partial class TypeChecker
{
    /// <summary>
    /// Types an expression and everything inside it. Returns the expression, rewritten if something inside
    /// it was resolved; its type, when known, is in the table under the returned node.
    /// </summary>
    private IrExpression Visit(IrExpression expression)
    {
        var (result, type) = Compute(expression);
        if (type != null) types.Set(result, type);
        return result;
    }

    /// <summary>The type of an expression returned by <see cref="Visit"/>; null if it is not known.</summary>
    private ITypeRef? TypeOf(IrExpression visited) => types.Get(visited);

    private (IrExpression Node, ITypeRef? Type) Compute(IrExpression expression)
    {
        switch (expression)
        {
            case IrComptimeInt:
                return (expression, new ComptimeIntegerTypeRef());

            case IrComptimeDecimal:
                return (expression, new ComptimeDecimalTypeRef());

            case IrComptimeBool:
                return (expression, BoolType());

            case IrNull:
                return (expression, new NullTypeRef());

            case IrStringConstant:
                return (expression, StringType());

            case IrBinary binary:
                return VisitBinary(binary);

            case IrUnary unary:
                return VisitUnary(unary);

            case IrIndex index:
                return VisitIndex(index);

            case IrStringConcat concat:
                return (concat with { Parts = [..concat.Parts.Select(Visit)] }, StringType());

            case IrReference reference:
                return VisitReference(reference);

            case IrLocal local:
                return (local, LocalValueType(local));

            case IrAssign assign:
                return VisitAssign(assign);

            case IrCall call:
                return VisitCall(call);

            case IrNew creation:
            {
                var visited = creation with { ArgumentsList = VisitArguments(creation.ArgumentsList) };
                return (ResolveConstructor(visited) ?? visited, TypeFromExpression(creation.Type));
            }

            case IrArray array:
                return VisitArray(array);

            case IrConvert conversion:
                return (conversion with { Expression = Visit(conversion.Expression) },
                    TypeFromExpression(conversion.toType));

            case IrAccess access:
                return VisitAccess(access);

            default:
                return (expression, null);
        }
    }

    private IrArgumentsList VisitArguments(IrArgumentsList list) =>
        list with { arguments = [..list.arguments.Select(Visit)] };

    /// <summary>
    /// `[a, b]` is a slice of the type its elements share. Literals have no size, so they make a slice of the
    /// default integer; elements of different types, or of unknown type, leave the type unknown.
    /// </summary>
    private (IrExpression, ITypeRef?) VisitArray(IrArray array)
    {
        var elements = array.Elements.Select(Visit).ToArray();
        var visited = array with { Elements = elements };
        if (elements.Length == 0) return (visited, null);

        var first = TypeOf(elements[0]);
        if (first == null || elements.Any(element => !Equals(TypeOf(element), first))) return (visited, null);

        ITypeRef element = first switch
        {
            ComptimeIntegerTypeRef => new StructTypeReference(CompilerStd.Types.Natural(true)),
            ComptimeDecimalTypeRef => new StructTypeReference(CompilerStd.Types.FloatOf(64)),
            _ => first,
        };
        return (visited, new SliceTypeRef(element));
    }

    /// <summary>
    /// `value.name`: the member is looked up in the type of the value, including what the type extends.
    /// A type that does not list its members is left alone; a type that does and has no such member is an error.
    /// </summary>
    private (IrExpression, ITypeRef?) VisitAccess(IrAccess access)
    {
        var asTarget = TakeTarget();
        var baseExpr = Visit(access.baseExpr);
        var visited = access with { baseExpr = baseExpr };

        if (access.access.Reference is not UnsolvedRef name)
        {
            return access.access.Reference is PropertyReference { Property: var known }
                ? PropertyAccess(visited, known, asTarget)
                : (visited, TypeOfReference(access.access.Reference));
        }

        // The length of a slice is an `int`; it is not a member of any struct, so it stays an unresolved name
        // and the backend knows it by the type of the value.
        if (TypeOf(baseExpr) is SliceTypeRef && name.Value == "len")
        {
            if (asTarget) Error("The length of a slice cannot be assigned", access.access.Location);
            return (visited, new StructTypeReference(CompilerStd.Types.Natural(true)));
        }

        // `u8.max` and `i32.min`: the bounds of an integer type. They are constants of that type (not literals: they
        // have a size), so `u8.max + 1` is a `u8` and overflows.
        if (name.Value is "min" or "max" && IsTypeOrNamespace(baseExpr) && TypeFromExpression(baseExpr) is { } denoted
            && (ConversionRules.IsInteger(denoted)))
        {
            if (asTarget) Error($"'{name.Value}' of '{TypeFormatter.Format(denoted)}' is a constant and cannot be assigned", access.access.Location);

            if (!IntegerBounds(denoted, out var minimum, out var maximum))
            {
                Error($"The bounds of '{TypeFormatter.Format(denoted)}' depend on the platform, so they have no value when compiling",
                    access.access.Location);
                return (visited, denoted);
            }

            return (new IrComptimeInt(access.Location, name.Value == "min" ? minimum : maximum), denoted);
        }

        var container = ContainerOf(TypeOf(baseExpr));
        if (container == null) return (visited, null);

        var member = container.TryGetMember(name.Value);
        if (member == null)
        {
            if (ListsMembers(container))
                Error($"'{TypeFormatter.Format(TypeOf(baseExpr))}' has no member named '{name.Value}'", access.access.Location);
            return (visited, null);
        }

        if (!AccessChecker.CanAccess(member, _accessor))
        {
            Error(AccessChecker.Explain(name.Value, member, _accessor), access.access.Location);
            return (visited, null);
        }

        var reference = member.GetReference();
        var resolved = visited with { access = new IrReference(access.access.Location, reference) };
        if (member is TqProperty property) return PropertyAccess(resolved, property, asTarget);

        // A field or property of `List(i8)` is typed in terms of `i8`, not of the `T` it was declared with.
        var memberType = TypeOfReference(reference);
        if (DependsOnTypeParameter(memberType))
        {
            var receiver = BindingsOf(TypeOf(baseExpr));
            if (receiver.Count > 0) memberType = Substitute(memberType, receiver);
        }

        return (resolved, memberType);
    }

    /// <summary>The smallest and the largest value of a sized integer type (`int` and `uint` are 32 bits); false for `iptr` and `uptr`.</summary>
    private static bool IntegerBounds(ITypeRef type, out BigInteger minimum, out BigInteger maximum)
    {
        minimum = maximum = 0;

        if (ConversionRules.TryInteger(type, out var signed, out var bits)) { }
        else if (ConversionRules.TryNatural(type, out signed)) bits = 32;
        else return false;

        minimum = signed ? -(BigInteger.One << (bits - 1)) : BigInteger.Zero;
        maximum = signed ? (BigInteger.One << (bits - 1)) - 1 : (BigInteger.One << bits) - 1;
        return true;
    }

    /// <summary>What has the members of a value of this type; null if the type has none known.</summary>
    private static IMemberContainer? ContainerOf(ITypeRef? type) => type switch
    {
        // A reference has the members of what it refers to.
        StructTypeReference when CompilerStd.Types.IsRef(type, out var referred) => ContainerOf(referred),
        StructTypeReference { Struct: GenericInstance instance } => instance.Definition as IMemberContainer,
        StructTypeReference { Struct: IMemberContainer container } => container,
        TypedefTypeReference typedef => typedef.Typedef,
        ProtocolTypeReference { Protocol: IMemberContainer container } => container,
        _ => null,
    };

    /// <summary>Whether a missing member is an error: the container must know all of its members.</summary>
    private static bool ListsMembers(IMemberContainer container) => container switch
    {
        TqStruct or TqTypedef => true,
        DotnetClass { Members: not null } or DotnetStruct { Members: not null } => true,
        DotnetEnum { Members: not null } or DotnetInterface { Members: not null } => true,
        _ => false,
    };

    private (IrExpression, ITypeRef?) VisitAssign(IrAssign assign)
    {
        if (assign.Target is IrIndex indexTarget) return VisitIndexAssign(assign, indexTarget);

        var source = Visit(assign.Source);
        var sourceType = TypeOf(source);

        // `let a = 1`: a local without a declared type takes the type of its value.
        // A literal has no size, so the variable gets the default integer.
        if (assign.Target is IrLocal local && NeedsInferredType(local) && sourceType != null)
        {
            if (sourceType is NullTypeRef)
                Error($"Cannot infer the type of '{local.name}' from null; declare it, as in `let ?i32 {local.name} = null`", source.Location);
            else
                types.Set(local, DefaultTypeOf(sourceType));
        }

        _targetNext = assign.Target is IrAccess or IrReference;
        var target = Visit(assign.Target);
        _targetNext = false;

        if (PropertyOf(target) is { } written) return WriteProperty(assign, target, source, written);

        // The declaration `const x = 1` gives the constant its value; nothing else writes to it.
        if (ConstantNameOf(target) is { } constant)
        {
            Error($"'{constant}' is a constant and cannot be assigned", assign.Location);
            return (assign with { Target = target, Source = source }, TypeOf(target));
        }

        var targetType = TypeOf(target);

        // Assigning to something whose type has an `@onset` function calls it instead (a declaration initializes).
        if (assign.Target is not IrLocal && TryOnSet(assign, target, source, targetType) is { } onset) return onset;

        // A local that took its type from the value needs no check; any other target keeps its own type.
        if (!(assign.Target is IrLocal declaredLocal && NeedsInferredType(declaredLocal)))
        {
            var typeExpression = target is IrLocal { type: { } declared } ? WithoutConst(declared) : null;
            source = Coerce(source, targetType, typeExpression, "the assignment");
        }

        return (assign with { Target = target, Source = source }, targetType);
    }

    private (IrExpression, ITypeRef?) VisitCall(IrCall call)
    {
        var visited = call with
        {
            callee = Visit(call.callee),
            ArgumentsList = VisitArguments(call.ArgumentsList),
        };

        // `a.f()` keeps `a` as the receiver; a function reached through a value is not static.
        // `Console.f()` and `Foo.f()` go through a namespace or a type, not a value: there the static ones apply.
        var viaValue = visited.callee is IrAccess { baseExpr: var receiver } && !IsTypeOrNamespace(receiver);

        switch (CalleeReference(visited.callee))
        {
            case FunctionGroupTypeReference group:
                visited = ResolveOverload(visited, group.FunctionGroup.Name, OverloadsOf(group.FunctionGroup, viaValue)) ?? visited;
                break;

            // A single function is a group of one: its arguments are checked and converted all the same.
            case FunctionTypeReference { Function: TqFunction function }:
                visited = ResolveOverload(visited, function.Name, [ToOverload(function)]) ?? visited;
                break;

            case FunctionTypeReference { Function: DotnetMethod { IsConstructor: false } method }:
                visited = ResolveOverload(visited, method.Name, [ToOverload(method)]) ?? visited;
                break;
        }

        return (visited, ReturnTypeOf(visited.callee, visited.ArgumentsList.arguments));
    }

    /// <summary>Whether an expression names a namespace (a static class of .NET is one) or a type, rather than being a value.</summary>
    private static bool IsTypeOrNamespace(IrExpression expression) =>
        (expression switch
        {
            IrReference reference => reference.Reference,
            IrAccess access => access.access.Reference,
            _ => null,
        }) is NamespaceReference or TypeMetaReference or StructTypeReference or ProtocolTypeReference or TypedefTypeReference;

    /// <summary>What a callee refers to: a name, or a member reached through a value.</summary>
    private static Ref? CalleeReference(IrExpression callee) => callee switch
    {
        IrReference reference => reference.Reference,
        IrAccess { access.Reference: var member } => member,
        _ => null,
    };

    /// <summary>
    /// The type of a call to a single function is its return type. A call to an overload group that could not be
    /// resolved yet has no known type.
    /// </summary>
    private ITypeRef? ReturnTypeOf(IrExpression callee)
    {
        if (CalleeReference(callee) is not FunctionTypeReference { Function: var function }) return null;

        // What it returns in terms of the type arguments of the value it is called on (`T` of `List(i8)` is `i8`).
        var returned = FunctionSignature.Of(function)?.Return;
        if (!DependsOnTypeParameter(returned)) return returned;

        var receiver = ReceiverBindings(callee);
        return receiver.Count > 0 ? Substitute(returned, receiver) : returned;
    }

    private ITypeRef? TypeOfReference(Ref reference) => reference switch
    {
        LocalReference { Local: var local } => LocalValueType(local),
        ParameterReference { Parameter: var parameter } => TypeFromExpression(parameter.Type),
        PropertyReference { Property: var property } => PropertyTypeOf(property),
        FieldReference { Field: TqField field } => FieldValueType(field),
        FieldReference { Field: DotnetField field } => field.Type,
        FieldReference { Field: DotnetProperty property } => property.Type,
        ThisReference { Owner: TqTypedef typedef } => new TypedefTypeReference(typedef),
        ThisReference { Owner: IStruct owner } => StructSemantics.TypeOf(owner),
        FieldReference { Field: TqNamedCase { Typedef: { } typedef } } => new TypedefTypeReference(typedef),
        // A type used as a value (for a `type T` parameter) has the type `Type`.
        TypeMetaReference => new StructTypeReference(CompilerStd.Types.Type),
        _ => null,
    };

    private static bool IsBool(ITypeRef type) => ConversionRules.IsStruct(type, CompilerStd.Types.Bool);

    private static StructTypeReference BoolType() => new(CompilerStd.Types.Bool);
    private static StructTypeReference StringType() => new(CompilerStd.Types.String);
    private static StructTypeReference VoidType() => new(CompilerStd.Types.Void);
}
