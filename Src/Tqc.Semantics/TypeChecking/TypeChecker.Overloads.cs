using Tqc.Core;
using Tqc.Dotnet.ModuleBuilder;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>One function of an overload group, with the types of its parameters (null when unknown).</summary>
internal sealed record Overload(
    IFunction Function,
    IReadOnlyList<ITypeRef?> ParameterTypes,
    IReadOnlyList<IrExpression?> ParameterTypeExpressions,
    int RequiredCount)
{
    public bool Accepts(int argumentCount) => argumentCount >= RequiredCount && argumentCount <= ParameterTypes.Count;
}

internal sealed partial class TypeChecker
{
    private sealed record Candidate(Overload Overload, ArgumentMatch[] Matches);

    /// <summary>
    /// Picks the function of the overloads that a call uses and returns the call pointing at it, with the
    /// arguments converted to the parameter types. Null when the call stays unresolved: either no overload
    /// fits (an error was reported) or there is not enough type information yet.
    /// </summary>
    private IrCall? ResolveOverload(IrCall call, string name, List<Overload> overloads)
    {
        var arguments = call.ArgumentsList.arguments;
        var winner = ChooseOverload(name, "overload", overloads, arguments, call.Location);
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

        if (structure is IObjectType { IsAbstract: true } abstractType)
        {
            Error($"'{structure.Name}' is {(abstractType.IsInterface ? "an interface" : "abstract")} and cannot be created",
                creation.Location);
            return null;
        }

        switch (structure)
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
                constructors = [..group.Methods.Where(method => method.IsConstructor).Select(ToOverload)];
                break;

            case DotnetStruct { Members: { } members } when members.GetValueOrDefault(".ctor") is DotnetMethodGroup group:
                constructors = [..group.Methods.Where(method => method.IsConstructor).Select(ToOverload)];
                break;

            default:
                return null;
        }

        var winner = ChooseOverload(structure.Name, "constructor", constructors, arguments, creation.Location);
        if (winner == null) return null;

        return creation with
        {
            Constructor = winner.Overload.Function,
            ArgumentsList = creation.ArgumentsList with { arguments = ConvertArguments(arguments, winner) },
        };
    }

    /// <summary>The best of the overloads for these arguments; null if none, or if it cannot be told yet.</summary>
    private Candidate? ChooseOverload(
        string name, string noun, List<Overload> overloads, IrExpression[] arguments, SourceLocation location)
    {
        var argumentTypes = arguments.Select(TypeOf).ToArray();
        var allOverloads = overloads;
        if (overloads.Count == 0)
        {
            Error($"'{name}' has no {noun} that the namespace '{_accessor.Namespace}' can reach", location);
            return null;
        }

        // A generic function takes its signature from the types the call gives.
        overloads = [..overloads.Select(overload => Specialize(overload, arguments, location))];

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
        if (best.Count != 1)
        {
            ReportAmbiguity(name, noun, candidates, arguments, location);
            return null;
        }

        return best[0];
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
        var valueType = TypeOf(value);
        if (valueType == null || target == null) return value;

        var match = ConversionRules.Classify(valueType, value, target, conversions, _accessor);
        if (match.Kind == MatchKind.None)
        {
            var hint = valueType is ComptimeIntegerTypeRef ? "; the literal does not fit or is not an integer type" : "; use `as` to convert it";
            Error($"Cannot convert '{TypeFormatter.Format(valueType)}' to '{TypeFormatter.Format(target)}' in {where}{hint}",
                value.Location);
            return value;
        }

        return ConvertTo(value, match, target, targetExpression);
    }

    private void ReportAmbiguity(
        string name, string noun, List<Candidate> candidates, IrExpression[] arguments, SourceLocation location)
    {
        var tied = string.Join(", ", candidates.Select(candidate => Signature(name, candidate.Overload)));
        var hint = arguments.Any(argument => argument is IrComptimeInt)
            ? " Give the literal an explicit type, for example `5 as i32`."
            : "";

        Error($"The call to the {noun}s of '{name}' is ambiguous between: {tied}.{hint}", location);
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
                foreach (var method in dotnetGroup.Methods.Where(method => method.IsStatic != viaValue && !method.IsConstructor))
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
            parameters.Count(parameter => parameter.DefaultValue == null));
    }

    private static Overload ToOverload(DotnetMethod method)
    {
        var parameterTypes = FunctionSignature.Of(method)!.Parameters;

        return new Overload(
            method,
            parameterTypes,
            [..parameterTypes.Select(_ => (IrExpression?)null)],
            parameterTypes.Count);
    }

    private static string Candidates(string name, List<Overload> overloads) =>
        "Candidates: " + string.Join(", ", overloads.Select(overload => Signature(name, overload)));

    private static string Signature(string name, Overload overload) =>
        $"{name}({string.Join(", ", overload.ParameterTypes.Select(TypeFormatter.Format))})";
}
