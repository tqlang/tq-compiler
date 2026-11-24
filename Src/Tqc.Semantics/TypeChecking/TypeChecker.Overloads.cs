using Tqc.Core;
using Tqc.Dotnet.ModuleBuilder;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>One function of an overload group, with the types of its parameters (null when unknown).</summary>
internal sealed record Overload(
    IFunction Function,
    IReadOnlyList<ITypeRef?> ParameterTypes,
    IReadOnlyList<IrExpression?> ParameterTypeExpressions,
    int RequiredCount,
    ITypeRef? Return = null)
{
    public bool Accepts(int argumentCount) => argumentCount >= RequiredCount && argumentCount <= ParameterTypes.Count;
}

internal sealed partial class TypeChecker
{
    private sealed record Candidate(Overload Overload, ArgumentMatch[] Matches);

    /// <summary>What the place a call is in does with its value: nothing (a statement), or it has to be of a type.</summary>
    private readonly record struct Expectation(bool Discarded, ITypeRef? Type)
    {
        public static Expectation Discard => new(true, null);
        public static Expectation Of(ITypeRef? type) => new(false, type);
    }

    /// <summary>
    /// Set for the call that is about to be visited, by whoever knows what is done with its value; the call takes it first
    /// thing, so that the calls inside its arguments do not see it. It only breaks ties between overloads that take the
    /// arguments equally well: the types of the arguments come first.
    /// </summary>
    private Expectation? _expected;

    private IrExpression VisitExpecting(IrExpression expression, Expectation expectation)
    {
        if (expression is IrCall) _expected = expectation;
        try { return Visit(expression); }
        finally { _expected = null; }
    }

    /// <summary>
    /// Picks the function of the overloads that a call uses and returns the call pointing at it, with the
    /// arguments converted to the parameter types. Null when the call stays unresolved: either no overload
    /// fits (an error was reported) or there is not enough type information yet.
    /// </summary>
    private IrCall? ResolveOverload(IrCall call, string name, List<Overload> overloads, Expectation? expected = null)
    {
        var arguments = call.ArgumentsList.arguments;
        var winner = ChooseOverload(name, "overload", overloads, arguments, call.Location, ReceiverBindings(call.callee), expected);
        if (winner == null) return null;

        var function = new FunctionTypeReference(winner.Overload.Function);
        return call with
        {
            callee = call.callee switch
            {
                IrAccess access => access with { access = new IrReference(access.access.Location, function) },
                _ => new IrReference(call.callee.Location, function),
            },
            ArgumentsList = call.ArgumentsList with { arguments = ConvertArguments(arguments, winner) },
        };
    }

    /// <summary>
    /// Picks the constructor a `new` uses, the same way as for a call. A struct of Tq without constructors
    /// takes no arguments. Null when it stays unresolved: an error was reported, the type is not known well
    /// enough (a generic instance, a .NET struct), or there is not enough type information yet.
    /// </summary>
    private IrNew? ResolveConstructor(IrNew creation)
    {
        if (creation.Type is not IrReference { Reference: TypeMetaReference { Type: IStruct structure } }) return null;

        var arguments = creation.ArgumentsList.arguments;
        List<Overload> constructors;

        // `List(i8)` is created with the constructors of `List`, whose `T` is `i8`.
        var definition = structure is GenericInstance instance ? instance.Definition : structure;

        if (definition is IObjectType { IsAbstract: true } abstractType)
        {
            Error($"'{structure.Name}' is {(abstractType.IsInterface ? (abstractType is TqStruct ? "a protocol" : "an interface") : "abstract")} and cannot be created",
                creation.Location);
            return null;
        }

        switch (definition)
        {
            case TqStruct { Constructors.Count: 0 } tq:
                if (arguments.Length > 0)
                    Error($"'{tq.Name}' declares no constructor, so it is created without arguments", creation.Location);
                return null;

            case TqStruct tq:
                constructors = [..tq.Constructors
                    .Where(constructor => AccessChecker.CanAccess(constructor, _accessor))
                    .Select(ToOverload)];
                break;

            case DotnetClass { Members: { } members } when members.GetValueOrDefault(".ctor") is DotnetMethodGroup group:
                constructors = [..group.Methods.Where(method => method.IsConstructor && IsCallable(method)).Select(ToOverload)];
                break;

            case DotnetStruct { Members: { } members } when members.GetValueOrDefault(".ctor") is DotnetMethodGroup group:
                constructors = [..group.Methods.Where(method => method.IsConstructor && IsCallable(method)).Select(ToOverload)];
                break;

            default:
                return null;
        }

        var winner = ChooseOverload(structure.Name, "constructor", constructors, arguments, creation.Location,
            BindingsOf(new StructTypeReference(structure)));
        if (winner == null) return null;

        return creation with
        {
            Constructor = winner.Overload.Function,
            ArgumentsList = creation.ArgumentsList with { arguments = ConvertArguments(arguments, winner) },
        };
    }

    /// <summary>The best of the overloads for these arguments; null if none, or if it cannot be told yet.</summary>
    private Candidate? ChooseOverload(
        string name, string noun, List<Overload> overloads, IrExpression[] arguments, SourceLocation location,
        Dictionary<TqParameter, ITypeRef>? receiver = null, Expectation? expected = null)
    {
        var argumentTypes = arguments.Select(TypeOf).ToArray();
        var allOverloads = overloads;
        if (overloads.Count == 0)
        {
            Error($"'{name}' has no {noun} that the namespace '{_accessor.Namespace}' can reach", location);
            return null;
        }

        // A generic function takes its signature from the types the call gives.
        overloads = [..overloads.Select(overload => Specialize(overload, arguments, location, receiver))];

        var byArity = overloads.Where(overload => overload.Accepts(arguments.Length)).ToList();
        if (byArity.Count == 0)
        {
            Error($"No {noun} of '{name}' takes {arguments.Length} argument(s). {Candidates(name, allOverloads)}", location);
            return null;
        }

        var candidates = new List<Candidate>();
        foreach (var overload in byArity)
        {
            var matches = arguments
                .Select((argument, i) => ConversionRules.Classify(
                    argumentTypes[i], argument, overload.ParameterTypes[i], conversions, _accessor))
                .ToArray();

            if (matches.All(match => match.Kind != MatchKind.None)) candidates.Add(new Candidate(overload, matches));
        }

        if (candidates.Count == 0)
        {
            var given = string.Join(", ", argumentTypes.Select(TypeFormatter.Format));
            Error($"No {noun} of '{name}' accepts ({given}). {Candidates(name, allOverloads)}", location);
            return null;
        }

        if (candidates.Count == 1) return candidates[0];

        // Without all the types the best candidate cannot be told yet; the call stays as it is.
        if (candidates.Any(candidate => candidate.Matches.Any(match => match.Kind == MatchKind.Unknown))) return null;

        var best = candidates.Where(candidate => candidates.All(other => ReferenceEquals(other, candidate) || Dominates(candidate, other))).ToList();
        if (best.Count == 1) return best[0];

        // The arguments do not decide: what is done with the value of the call does. Only candidates that fit the arguments
        // exactly as well as each other are left to it.
        var open = candidates.Where(candidate => !candidates.Any(other => Dominates(other, candidate))).ToList();
        if (expected is { } expectation && open.Count > 1 && open.All(candidate => SameCosts(candidate, open[0])))
        {
            var byReturn = ChooseByReturn(open, expectation);
            if (byReturn.Count == 1) return byReturn[0];
            if (byReturn.Count > 1) open = byReturn;
        }

        ReportAmbiguity(name, noun, open.Count > 1 ? open : candidates, arguments, location, expected);
        return null;
    }

    private static bool SameCosts(Candidate a, Candidate b) =>
        a.Matches.Zip(b.Matches).All(pair => pair.First.Cost == pair.Second.Cost);

    /// <summary>
    /// The candidates whose return type suits what the call is for, the best fit only. A statement that throws the value away
    /// prefers a function that returns nothing; a place that needs a type takes the ones that return it (exactly before with a
    /// conversion), and not the ones that return something else. A return type that is not known fits, but not best.
    /// </summary>
    private List<Candidate> ChooseByReturn(List<Candidate> candidates, Expectation expected)
    {
        var ranked = new List<(Candidate Candidate, int Rank)>();

        foreach (var candidate in candidates)
        {
            var returned = candidate.Overload.Return;
            var isVoid = returned != null && ConversionRules.IsStruct(returned, CompilerStd.Types.Void);

            int? rank;
            if (expected.Discarded) rank = isVoid ? 0 : 1;
            else if (returned == null || expected.Type == null) rank = 1;
            else
            {
                var fit = ConversionRules.Classify(returned, new IrNull(default), expected.Type, conversions, _accessor);
                rank = fit.Kind switch
                {
                    MatchKind.Exact => 0,
                    MatchKind.Convert => 1 + fit.Cost,
                    _ => null,
                };
            }

            if (rank is { } value) ranked.Add((candidate, value));
        }

        if (ranked.Count == 0) return [];
        var lowest = ranked.Min(entry => entry.Rank);
        return [..ranked.Where(entry => entry.Rank == lowest).Select(entry => entry.Candidate)];
    }

    /// <summary>`a` is better when it fits every argument at least as well as `b` and one of them better.</summary>
    private static bool Dominates(Candidate a, Candidate b)
    {
        var better = false;
        for (var i = 0; i < a.Matches.Length; i++)
        {
            if (a.Matches[i].Cost > b.Matches[i].Cost) return false;
            if (a.Matches[i].Cost < b.Matches[i].Cost) better = true;
        }

        return better;
    }

    /// <summary>The arguments, each converted to the type of its parameter where that takes a conversion.</summary>
    private IrExpression[] ConvertArguments(IrExpression[] original, Candidate winner)
    {
        var arguments = original.ToArray();

        for (var i = 0; i < arguments.Length; i++)
        {
            arguments[i] = ConvertTo(
                arguments[i], winner.Matches[i], winner.Overload.ParameterTypes[i]!,
                winner.Overload.ParameterTypeExpressions[i]);
        }

        return arguments;
    }

    /// <summary>
    /// The value converted to `type` when that takes a conversion: a call to an `@implicitConvert` function, or an
    /// `IrConvert` (widening, a literal given its size, a reference to what its struct extends).
    /// </summary>
    private IrExpression ConvertTo(IrExpression value, ArgumentMatch match, ITypeRef type, IrExpression? typeExpression)
    {
        if (match.Kind != MatchKind.Convert) return value;

        var location = value.Location;

        // `T` where `?T` or `!T` is asked: the value is brought to `T` and then wrapped.
        if (match.Wraps && (CompilerStd.Types.IsNullable(type, out var wrapped) || CompilerStd.Types.IsFailable(type, out wrapped)))
        {
            var innerMatch = ConversionRules.Classify(TypeOf(value), value, wrapped, conversions, _accessor);
            var inner = ConvertTo(value, innerMatch, wrapped, null);

            typeExpression ??= TypeExpressionOf(type, location);
            if (typeExpression == null) return inner;

            var wrapper = new IrConvert(location, inner, typeExpression);
            types.Set(wrapper, type);
            return wrapper;
        }

        // `null` is not converted: it is the `null` of that `?T`.
        if (value is IrNull)
        {
            var typed = new IrNull(location);
            types.Set(typed, type);
            return typed;
        }

        if (match.Via is { } conversion)
        {
            var conversionCall = new IrCall(
                location,
                new IrReference(location, new FunctionTypeReference(conversion)),
                new IrArgumentsList(location, [value]));
            types.Set(conversionCall, type);
            return conversionCall;
        }

        typeExpression ??= TypeExpressionOf(type, location);
        if (typeExpression == null) return value;

        var converted = new IrConvert(location, value, typeExpression);
        types.Set(converted, type);
        return converted;
    }

    /// <summary>
    /// The value of an assignment, a return, a field or a default, which has to be of `target`: converted when
    /// that is lossless, an error otherwise (`as` converts explicitly). Unknown types are left alone.
    /// </summary>
    private IrExpression Coerce(IrExpression value, ITypeRef? target, IrExpression? targetExpression, string where)
    {
        // The name of a static function where a delegate of .NET is expected: `let *Action a = f`.
        if (DelegateOf(target) is { } delegateInfo && FunctionsNamed(value) != null)
            return FunctionAsDelegate(value, target!, delegateInfo, where) ?? value;

        var valueType = TypeOf(value);
        if (valueType == null || target == null) return value;

        // The arms of a match whose value is a literal take the type that is asked for: `let u8 x = match v { case 1 => 10  default => 0 }`.
        if (value is IrMatch literalMatch && valueType is ComptimeIntegerTypeRef or ComptimeDecimalTypeRef)
        {
            var retyped = literalMatch with
            {
                Arms = [..literalMatch.Arms.Select(arm => arm with
                {
                    Body = arm.Body is IrExpression armValue ? Coerce(armValue, target, null, where) : arm.Body,
                })],
            };
            types.Set(retyped, target);
            return retyped;
        }

        // A literal takes the type it is given: `let [3]u8 x = [1, 2, 3]` has `u8` elements, and `&[1, 2]` for a `[]u8` too.
        if (target is ArrayTypeRef literalTarget && value is IrArray literal && literal.Elements.Length == literalTarget.Length)
            return RetypeLiteral(literal, literalTarget, where);

        if (target is SliceTypeRef sliceTarget && value is IrAddressOf { Operand: IrArray inner } addressOf)
        {
            var retyped = RetypeLiteral(inner, new ArrayTypeRef(inner.Elements.Length, sliceTarget.Element), where);
            var address = addressOf with { Operand = retyped };
            types.Set(address, target);
            return address;
        }

        var match = ConversionRules.Classify(valueType, value, target, conversions, _accessor);
        if (match.Kind == MatchKind.None)
        {
            var hint = valueType is ComptimeIntegerTypeRef ? "; the literal does not fit or is not an integer type"
                : valueType is NullTypeRef ? "; only a nullable type ('?T') can be null"
                : "; use `as` to convert it";
            Error($"Cannot convert '{TypeFormatter.Format(valueType)}' to '{TypeFormatter.Format(target)}' in {where}{hint}",
                value.Location);
            return value;
        }

        return ConvertTo(value, match, target, targetExpression);
    }

    private IrExpression RetypeLiteral(IrArray literal, ArrayTypeRef target, string where)
    {
        var retyped = literal with { Elements = [..literal.Elements.Select(element => Coerce(element, target.Element, null, where))] };
        types.Set(retyped, target);
        return retyped;
    }

    private void ReportAmbiguity(
        string name, string noun, List<Candidate> candidates, IrExpression[] arguments, SourceLocation location, Expectation? expected = null)
    {
        // Two that only differ in what they return are told apart by what is done with the value.
        var written = candidates.Select(candidate => Signature(name, candidate.Overload)).ToList();
        var sameParameters = written.Distinct().Count() == 1;
        var tied = string.Join(", ", candidates.Select(candidate => Signature(name, candidate.Overload, withReturn: true)));

        var context = expected switch
        {
            null => "the value is used where no type is asked (an argument, an operand, an assignment to a variable that exists)",
            { Discarded: true } => "the value is thrown away",
            { Type: { } wanted } => $"a '{TypeFormatter.Format(wanted)}' is wanted",
            _ => "the type that is wanted is not known",
        };

        var hint = sameParameters
            ? " They only differ in what they return, and nothing here says which one is wanted: use the value where its type is known"
                + " (`let T x = ...`, `return ...`), or call it as a statement to run the one that returns nothing."
            : arguments.Any(argument => argument is IrComptimeInt)
                ? " Give the literal an explicit type, for example `5 as i32`."
                : "";

        Error($"The call to the {noun}s of '{name}' is ambiguous between: {tied}.{hint} [here: {context}]", location);
    }

    /// <param name="viaValue">The call goes through a value (`a.f()`): methods of instances, not static ones.</param>
    private List<Overload> OverloadsOf(IFunctionGroup group, bool viaValue)
    {
        var overloads = new List<Overload>();

        switch (group)
        {
            case TqFunctionGroup tqGroup:
                overloads.AddRange(tqGroup
                    .Where(candidate => AccessChecker.CanAccess(candidate, _accessor))
                    .Where(candidate => !viaValue || !candidate.IsStatic)
                    .Select(ToOverload));
                break;

            case DotnetMethodGroup dotnetGroup:
                // Through a type only static methods apply; through a value, only those of instances.
                foreach (var method in dotnetGroup.Methods.Where(method => method.IsStatic != viaValue && !method.IsConstructor && IsCallable(method)))
                    overloads.Add(ToOverload(method));
                break;
        }

        return overloads;
    }

    /// <summary>A type as an expression, to say what an argument is converted to.</summary>
    private static IrExpression? TypeExpressionOf(ITypeRef type, SourceLocation location) => type switch
    {
        // `Ref(Foo)`, `!i32`: applications of a pseudotype are references to their entry in the instance table.
        StructTypeReference { Struct: GenericInstance instance } => new IrReference(location, instance.GetReference()),
        _ => TypeExpressions.Of(type, location),
    };

    private static Overload ToOverload(TqFunction function)
    {
        var parameters = function.Parameters;
        return new Overload(
            function,
            FunctionSignature.Of(function)!.Parameters,
            [..parameters.Select(parameter => (IrExpression?)parameter.Type)],
            parameters.Count(parameter => parameter.DefaultValue == null),
            FunctionSignature.Of(function)!.Return);
    }

    /// <summary>
    /// Whether Tq can call a method of .NET. One that takes a type with no equivalent in Tq (`char[]`, `Span<T>`...)
    /// cannot be given any argument, so it is not a candidate: left in, its unknown parameter would match everything
    /// and make a call with several overloads impossible to decide.
    /// </summary>
    private static bool IsCallable(DotnetMethod method) => !method.ParameterTypes.Any(type => type is UnsupportedTypeRef);

    private static Overload ToOverload(DotnetMethod method)
    {
        var parameterTypes = FunctionSignature.Of(method)!.Parameters;

        return new Overload(
            method,
            parameterTypes,
            [..parameterTypes.Select(_ => (IrExpression?)null)],
            parameterTypes.Count,
            FunctionSignature.Of(method)!.Return);
    }

    private static string Candidates(string name, List<Overload> overloads) =>
        "Candidates: " + string.Join(", ", overloads.Select(overload => Signature(name, overload)));

    private static string Signature(string name, Overload overload, bool withReturn = false) =>
        $"{name}({string.Join(", ", overload.ParameterTypes.Select(TypeFormatter.Format))})"
        + (withReturn ? $" -> {(overload.Return == null ? "?" : TypeFormatter.Format(overload.Return))}" : "");
}
