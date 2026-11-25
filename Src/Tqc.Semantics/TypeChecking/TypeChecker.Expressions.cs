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
                CheckValueType(local.type);
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

            case IrRange range:
                return VisitRange(range);

            case IrMatch match:
                return VisitMatch(match);

            case IrAddressOf addressOf:
                return VisitAddressOf(addressOf);

            case IrConvert conversion:
                CheckValueType(conversion.toType);
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
    /// `[a, b]` is an array of as many elements as it has, of the type they share. Literals have no size, so they make an
    /// array of the default integer (a declaration with a type, as `let [2]u8 x = [1, 2]`, gives them its own: see Coerce);
    /// elements of different types, or of unknown type, leave the type unknown.
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
        return (visited, new ArrayTypeRef(elements.Length, element));
    }

    /// <summary>
    /// `match v { case a, b => x  default => y }`: each pattern is compared with `v` as in `v == a` (so a literal takes the type of
    /// `v`, and the operators of its type apply). The arms mean the same in any order, and `default` takes what no
    /// other arm does; it is required unless the arms already cover every value (`true` and `false` of a `bool`, all the values of a sized integer). The type is the one the arms share; it is not known when they differ or when an arm
    /// is a block, and then the match can only be a statement.
    /// </summary>
    private (IrExpression, ITypeRef?) VisitMatch(IrMatch match)
    {
        var subject = Visit(match.Subject);
        var subjectType = TypeOf(subject);

        // The subject can be a type (`match T { case i32 => ... }`, in a generic function): it is not kept, the patterns compare with it.
        var subjectIsType = subjectType is StructTypeReference { Struct: var subjectStruct } && ReferenceEquals(subjectStruct, CompilerStd.Types.Type);

        var local = new IrLocal(match.Location, null, "$match");
        if (subjectType != null && !subjectIsType) types.Set(local, DefaultTypeOf(subjectType));
        IrExpression held = subjectIsType ? subject : new IrReference(match.Location, new LocalReference(local));

        var subjectTypedef = (subjectType as TypedefTypeReference)?.Typedef;

        List<IrMatchArm> arms = [];
        List<ITypeRef?> values = [];
        var blocks = false;
        var defaults = 0;
        HashSet<object> literals = [];
        HashSet<bool> booleans = [];
        HashSet<string> cases = [];

        foreach (var arm in match.Arms)
        {
            List<IrExpression> patterns = [];
            List<IrExpression> conditions = [];
            var bindingType = false;
            foreach (var pattern in arm.Patterns)
            {
                // The order of the arms means nothing, so one value cannot be in two of them.
                object? literal = pattern switch
                {
                    IrComptimeInt integer => integer.BigInteger,
                    IrStringConstant text => text.Value,
                    _ => null,
                };
                if (literal != null && !literals.Add(literal))
                    Error("This value is already in another arm of the 'match'", pattern.Location);

                var visitedPattern = Visit(pattern);
                if (visitedPattern is IrComptimeBool boolean && !booleans.Add(boolean.Value))
                    Error("This value is already in another arm of the 'match'", pattern.Location);

                if (subjectTypedef != null && TypeOf(visitedPattern) is TypedefTypeReference { Typedef: var patternTypedef }
                    && ReferenceEquals(patternTypedef, subjectTypedef) && NamedCaseName(visitedPattern) is { } caseName
                    && !cases.Add(caseName))
                    Error("This value is already in another arm of the 'match'", pattern.Location);

                IrExpression comparison;
                if (TypeOf(visitedPattern) is StructTypeReference { Struct: var patternStruct } && ReferenceEquals(patternStruct, CompilerStd.Types.Type))
                {
                    // A type: `case Foo`.
                    comparison = new IrTypeTest(pattern.Location, held, visitedPattern, subjectIsType);
                    types.Set(comparison, BoolType()!);
                    bindingType = !subjectIsType;

                    if (arm.Binding != null && !subjectIsType && TypeFromExpression(visitedPattern) is { } narrowed)
                        types.Set(arm.Binding, narrowed);
                }
                else
                {
                    if (subjectIsType)
                        Error("The patterns of a 'match' on a type are types", pattern.Location);

                    comparison = Visit(new IrBinary(pattern.Location, BinaryOperator.Equal, held, visitedPattern));

                    if (TypeOf(comparison) is { } comparisonType && !IsBool(comparisonType))
                        Error($"The pattern cannot be compared with the value of 'match' ('{TypeFormatter.Format(comparisonType)}')", pattern.Location);
                }

                patterns.Add(visitedPattern);
                conditions.Add(comparison);
            }

            if (arm.Binding != null && (arm.Patterns.Length != 1 || !bindingType))
                Error($"'{arm.Binding.name}' names the value as the type of the pattern, so the pattern has to be one type", arm.Binding.Location);

            if (arm.IsDefault && ++defaults > 1) Error("A 'match' has only one 'default'", arm.Location);

            IrNode body;
            if (arm.Body is IrBody block)
            {
                body = CheckNode(block);
                blocks = true;
            }
            else
            {
                var value = Visit((IrExpression)arm.Body);
                values.Add(TypeOf(value));
                body = value;
            }

            arms.Add(arm with { Patterns = [..patterns], Conditions = [..conditions], Body = body });
        }

        // What is not in any arm goes to `default`, wherever it is written; without one, the arms have to cover every value.
        if (defaults == 0 && !CoversEveryValue(subjectType, literals, booleans, cases))
            Error(subjectType != null && (IsBool(subjectType) || subjectTypedef != null || ConversionRules.TryInteger(subjectType, out _, out _))
                ? $"A 'match' needs a 'default' arm, or an arm for every value of '{TypeFormatter.Format(subjectType)}'"
                : "A 'match' needs a 'default' arm", match.Location);

        ITypeRef? type = null;
        if (!blocks && values.Count > 0 && values.All(value => value != null))
        {
            type = values[0];
            foreach (var value in values.Skip(1))
            {
                type = type != null ? ConversionRules.Common(type, value!) : null;
                if (type == null) break;
            }
        }

        return (match with { Subject = subject, Arms = [..arms], SubjectLocal = local }, type);
    }

    /// <summary>The name of the case a pattern like `Level.info` refers to.</summary>
    private static string? NamedCaseName(IrExpression pattern) => (pattern switch
    {
        IrAccess access => access.access.Reference,
        IrReference reference => reference.Reference,
        _ => null,
    }) switch
    {
        FieldReference { Field: TqNamedCase named } => named.Name,
        UnsolvedRef unsolved => unsolved.Value,
        _ => null,
    };

    /// <summary>Whether the patterns leave nothing for `default`: `true` and `false` of a `bool`, every case of a typedef, every value of a sized integer.</summary>
    private static bool CoversEveryValue(ITypeRef? subjectType, HashSet<object> literals, HashSet<bool> booleans, HashSet<string> cases)
    {
        if (subjectType == null) return false;

        // A typedef: every case it names (the numbers it takes with no name cannot be written in a pattern).
        if (subjectType is TypedefTypeReference { Typedef: var typedef })
        {
            var named = typedef.Members.Values.OfType<TqNamedCase>().ToList();
            return typedef.NumericCases.IsEmpty && named.Count > 0 && named.All(entry => cases.Contains(entry.Name));
        }

        if (IsBool(subjectType)) return booleans.Count == 2;

        if (!ConversionRules.TryInteger(subjectType, out var signed, out var bits)) return false;

        var low = signed ? -(BigInteger.One << (bits - 1)) : BigInteger.Zero;
        var high = signed ? (BigInteger.One << (bits - 1)) - 1 : (BigInteger.One << bits) - 1;
        return literals.OfType<BigInteger>().Count(value => value >= low && value <= high) == high - low + 1;
    }

    /// <summary>`a .. b`: both ends are integers, and the first is not after the last.</summary>
    private (IrExpression, ITypeRef?) VisitRange(IrRange range)
    {
        var from = Visit(range.From);
        var to = Visit(range.To);
        CheckPositionIndices([from, to]);

        if (from is IrComptimeInt { BigInteger: var first } && to is IrComptimeInt { BigInteger: var last } && first > last)
            Error($"The range '{first} .. {last}' ends before it starts (both ends are included)", range.Location);

        return (range with { From = from, To = to }, null);
    }

    /// <summary>`&array`: a slice that refers to the array, so what is written through one is seen by the other.</summary>
    private (IrExpression, ITypeRef?) VisitAddressOf(IrAddressOf addressOf)
    {
        var operand = Visit(addressOf.Operand);
        var visited = addressOf with { Operand = operand };

        switch (TypeOf(operand))
        {
            case null:
                return (visited, null);

            case ArrayTypeRef array:
                return (visited, new SliceTypeRef(array.Element));

            case SliceTypeRef:
                Error("A slice already is a reference: '&' takes an array", addressOf.Location);
                return (visited, null);

            // A struct by value that is a variable: a reference to it (the backend checks that it is of a hierarchy).
            case StructTypeReference { Struct: var referred } structType when !StructSemantics.IsReference(referred) && operand is IrReference { Reference: LocalReference or ParameterReference }:
                return (visited, CompilerStd.Types.RefOf(structType));

            case StructTypeReference { Struct: var referred } when StructSemantics.IsReference(referred):
                Error($"'{TypeFormatter.Format(TypeOf(operand)!)}' already is a reference: '&' takes an array, or a variable of a struct by value", addressOf.Location);
                return (visited, null);

            case var other:
                Error($"'&' takes an array or a variable of a struct by value, not '{TypeFormatter.Format(other)}'", addressOf.Location);
                return (visited, null);
        }
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
        if (TypeOf(baseExpr) is SliceTypeRef or ArrayTypeRef && name.Value == "len")
        {
            if (asTarget) Error("The length of a slice or an array cannot be assigned", access.access.Location);
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

        if (member is IntrinsicProperty { Owner: var declaring } && asTarget)
            Error($"'{name.Value}' of '{declaring.Name}' can only be read, not assigned", access.access.Location);

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
        PrimitiveStruct { Members.Count: > 0 } => true,
        _ => false,
    };

    private (IrExpression, ITypeRef?) VisitAssign(IrAssign assign)
    {
        if (assign.Target is IrIndex indexTarget) return VisitIndexAssign(assign, indexTarget);

        if (assign.Target is IrLocal { type: { } writtenType }) CheckValueType(writtenType);

        // `x.Event += f`, `d += f`: adds a function to what a delegate calls.
        if (VisitDelegateAssign(assign) is { } delegated) return delegated;

        // A call whose value goes to a variable declared with a type is chosen by what it returns: `let i32 x = f()`.
        var declaredType = assign.Target is IrLocal { type: { } typeWritten } declaring && !NeedsInferredType(declaring)
            ? TypeFromExpression(WithoutConst(typeWritten)!)
            : null;
        var source = declaredType != null ? VisitExpecting(assign.Source, Expectation.Of(declaredType)) : Visit(assign.Source);
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
        // Taken before anything inside the call is visited: it is about this call only.
        var expected = _expected;
        _expected = null;

        var visited = call with
        {
            callee = Visit(call.callee),
            ArgumentsList = VisitArguments(call.ArgumentsList),
        };

        // `a.f()` keeps `a` as the receiver; a function reached through a value is not static.
        // `Console.f()` and `Foo.f()` go through a namespace or a type, not a value: there the static ones apply.
        var viaValue = visited.callee is IrAccess { baseExpr: var receiver2 } && !IsTypeOrNamespace(receiver2);

        switch (CalleeReference(visited.callee))
        {
            case FunctionGroupTypeReference group:
                visited = ResolveOverload(visited, group.FunctionGroup.Name, OverloadsOf(group.FunctionGroup, viaValue), expected) ?? visited;
                break;

            // A single function is a group of one: its arguments are checked and converted all the same.
            case FunctionTypeReference { Function: TqFunction function }:
                visited = ResolveOverload(visited, function.Name, [ToOverload(function)], expected) ?? visited;
                break;

            // `x.unwrap()` of a `?T`, `x.ignored()` of a `!T`: declared by the compiler, lowered by the backend.
            case FunctionTypeReference { Function: IntrinsicMethod intrinsic }:
                visited = ResolveOverload(visited, intrinsic.Name, [ToOverload(intrinsic)], expected) ?? visited;
                break;

            case FunctionTypeReference { Function: DotnetMethod { IsConstructor: false } method }:
                visited = ResolveOverload(visited, method.Name, [ToOverload(method)], expected) ?? visited;
                break;
        }

        var returnType = ReturnTypeOf(visited.callee, visited.ArgumentsList.arguments);

        // A function that was chosen but whose `T` could not be replaced: say which, instead of leaving a call without a type.
        if (returnType == null
            && CalleeReference(visited.callee) is FunctionTypeReference { Function: var chosen }
            && FunctionSignature.Of(chosen)?.Return is { } declared && DependsOnTypeParameter(declared))
        {
            var receiverType = visited.callee is IrAccess access ? TypeOf(access.baseExpr) : null;
            var bound = BindingsOf(receiverType);
            var known = bound.Count == 0 ? "none" : string.Join(", ", bound.Select(pair => $"{pair.Key.Name} = {TypeFormatter.Format(pair.Value)}"));

            Error($"Cannot tell the type this call returns: '{chosen.Name}' returns '{TypeFormatter.Format(declared)}', " +
                  $"and the type arguments known here are: {known} (the value it is called on has the type '{TypeFormatter.Format(receiverType)}')",
                visited.Location);
        }

        return (visited, returnType);
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
        FieldReference { Field: DotnetEvent @event } => @event.Type,
        FieldReference { Field: IntrinsicProperty property } => property.Type,
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
