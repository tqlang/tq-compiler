using System.Collections.Immutable;
using Tqc.Core;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers.Tq;

namespace Tqc.Semantics;

[Flags]
internal enum AttributeTarget
{
    Field = 1,
    Function = 2,
    Struct = 4,
    Typedef = 8,
    Case = 16,
    Any = Field | Function | Struct | Typedef | Case,
}

/// <summary>The built-in attributes of the language and what each one accepts.</summary>
internal static class BuiltinAttributes
{
    private sealed record Spec(
        BuiltinAttribute Kind, AttributeTarget Targets, int MinArguments = 0, int MaxArguments = 0, bool Strings = false);

    private static readonly Dictionary<string, Spec> Specs = new()
    {
        // Access. The reference spells them `allowAcessTo`/`denyAcessTo`; the correct spelling is accepted too.
        ["public"] = new(BuiltinAttribute.Public, AttributeTarget.Any),
        ["internal"] = new(BuiltinAttribute.Internal, AttributeTarget.Any),
        ["private"] = new(BuiltinAttribute.Private, AttributeTarget.Any),
        ["allowAcessTo"] = new(BuiltinAttribute.AllowAccessTo, AttributeTarget.Any, 1, 1),
        ["allowAccessTo"] = new(BuiltinAttribute.AllowAccessTo, AttributeTarget.Any, 1, 1),
        ["denyAcessTo"] = new(BuiltinAttribute.DenyAccessTo, AttributeTarget.Any, 1, 1),
        ["denyAccessTo"] = new(BuiltinAttribute.DenyAccessTo, AttributeTarget.Any, 1, 1),
        ["static"] = new(BuiltinAttribute.Static, AttributeTarget.Any),
        ["defineGlobal"] = new(BuiltinAttribute.DefineGlobal, AttributeTarget.Any, 1, 1, Strings: true),

        // Object orientation
        ["class"] = new(BuiltinAttribute.Class, AttributeTarget.Struct),
        ["final"] = new(BuiltinAttribute.Final, AttributeTarget.Struct),
        ["abstract"] = new(BuiltinAttribute.Abstract, AttributeTarget.Struct),
        ["interface"] = new(BuiltinAttribute.Interface, AttributeTarget.Struct),
        ["valueOnly"] = new(BuiltinAttribute.ValueOnly, AttributeTarget.Struct),
        ["referenceOnly"] = new(BuiltinAttribute.ReferenceOnly, AttributeTarget.Struct),
        ["virtual"] = new(BuiltinAttribute.Virtual, AttributeTarget.Function),
        ["override"] = new(BuiltinAttribute.Override, AttributeTarget.Function),

        // Functions
        ["inline"] = new(BuiltinAttribute.Inline, AttributeTarget.Function),
        ["noInline"] = new(BuiltinAttribute.NoInline, AttributeTarget.Function),
        ["comptime"] = new(BuiltinAttribute.Comptime, AttributeTarget.Function),
        ["runtime"] = new(BuiltinAttribute.Runtime, AttributeTarget.Function),
        ["callConv"] = new(BuiltinAttribute.CallConv, AttributeTarget.Function, 1, 1),

        // Overloading
        ["getter"] = new(BuiltinAttribute.Getter, AttributeTarget.Function, 1, 1),
        ["setter"] = new(BuiltinAttribute.Setter, AttributeTarget.Function, 1, 1),
        ["indexerGet"] = new(BuiltinAttribute.IndexerGet, AttributeTarget.Function),
        ["indexerSet"] = new(BuiltinAttribute.IndexerSet, AttributeTarget.Function),
        ["implicitConvert"] = new(BuiltinAttribute.ImplicitConvert, AttributeTarget.Function),
        ["explicitConvert"] = new(BuiltinAttribute.ExplicitConvert, AttributeTarget.Function),
        ["overrideOperator"] = new(BuiltinAttribute.OverrideOperator, AttributeTarget.Function, 1, 1),
        ["defineAttribute"] = new(BuiltinAttribute.DefineAttribute, AttributeTarget.Function, 0, 1),

        // Layout
        ["packed"] = new(BuiltinAttribute.Packed, AttributeTarget.Struct, 1, 1),
        ["union"] = new(BuiltinAttribute.Union, AttributeTarget.Struct),
        ["off"] = new(BuiltinAttribute.Off, AttributeTarget.Field, 1, 1),
        ["lay"] = new(BuiltinAttribute.Lay, AttributeTarget.Field, 1, 1),
        ["pad"] = new(BuiltinAttribute.Pad, AttributeTarget.Field, 1, 1),
    };

    // Attributes of each group contradict each other.
    private static readonly BuiltinAttribute[][] ExclusiveGroups =
    [
        [BuiltinAttribute.Public, BuiltinAttribute.Internal, BuiltinAttribute.Private],
        [BuiltinAttribute.Comptime, BuiltinAttribute.Runtime],
        [BuiltinAttribute.Inline, BuiltinAttribute.NoInline],
        [BuiltinAttribute.Final, BuiltinAttribute.Abstract, BuiltinAttribute.Interface],
        [BuiltinAttribute.ValueOnly, BuiltinAttribute.ReferenceOnly],
    ];

    /// <summary>
    /// A known name becomes a built-in attribute if its arguments fit; an unknown one is kept as it is.
    /// Returns an error message when the arguments do not fit.
    /// </summary>
    public static (TqAttribute? Attribute, string? Error) Create(
        string name, IrExpression[] arguments, SourceLocation location)
    {
        ImmutableArray<IrExpression> immutableArguments = [..arguments];
        if (!Specs.TryGetValue(name, out var spec))
            return (new TqUnknownAttribute(name, immutableArguments, location), null);

        if (arguments.Length < spec.MinArguments || arguments.Length > spec.MaxArguments)
        {
            var expected = spec.MinArguments == spec.MaxArguments
                ? $"{spec.MinArguments}"
                : $"{spec.MinArguments} to {spec.MaxArguments}";
            return (null, $"Attribute '@{name}' takes {expected} argument(s), but {arguments.Length} were given");
        }

        if (spec.Strings && arguments.Any(argument => argument is not IrStringConstant))
            return (null, $"The argument of '@{name}' must be a string literal");

        return (new TqBuiltinAttribute(spec.Kind, name, immutableArguments, location), null);
    }

    /// <summary>Problems of the attributes of one member as a whole: wrong target, repeated or contradicting.</summary>
    public static IEnumerable<(string Message, SourceLocation Location)> Check(
        IReadOnlyList<TqAttribute> attributes, AttributeTarget target)
    {
        var builtins = attributes.OfType<TqBuiltinAttribute>().ToList();

        foreach (var attribute in builtins)
        {
            if ((Specs[attribute.Name].Targets & target) == 0)
                yield return ($"Attribute '@{attribute.Name}' cannot be applied to a {Describe(target)}", attribute.Location);
        }

        foreach (var group in builtins.GroupBy(attribute => attribute.Kind).Where(group => group.Count() > 1))
            yield return ($"Attribute '@{group.First().Name}' is repeated", group.Skip(1).First().Location);

        foreach (var exclusive in ExclusiveGroups)
        {
            var present = builtins.Where(attribute => exclusive.Contains(attribute.Kind)).DistinctBy(a => a.Kind).ToList();
            if (present.Count > 1)
                yield return ($"Attributes {string.Join(" and ", present.Select(a => "'@" + a.Name + "'"))} contradict each other",
                    present[1].Location);
        }
    }

    private static string Describe(AttributeTarget target) => target switch
    {
        AttributeTarget.Field => "field",
        AttributeTarget.Function => "function",
        AttributeTarget.Typedef => "typedef",
        AttributeTarget.Case => "case",
        _ => "struct",
    };
}
