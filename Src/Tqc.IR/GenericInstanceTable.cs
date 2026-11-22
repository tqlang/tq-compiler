using System.Collections.Immutable;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.References;

namespace Tqc.IR;

/// <summary>
/// Every application of a generic struct becomes an entry here, shared by the whole build
/// (the keywords of the compiler and the code of every module).
/// Applications with the same definition and the same literal or referenced arguments share one entry.
/// An argument that is not a literal or a reference (an expression) cannot be compared yet,
/// so it gets its own entry until the comptime step evaluates it.
/// </summary>
public sealed class GenericInstanceTable
{
    public static readonly GenericInstanceTable Shared = new();

    private readonly Dictionary<Key, GenericInstance> _byKey = [];
    private readonly List<GenericInstance> _all = [];

    public IReadOnlyList<GenericInstance> Instances => _all;

    public GenericInstance GetOrAdd(IStruct definition, ImmutableArray<IrExpression> arguments)
    {
        var key = Key.TryCreate(definition, arguments);
        if (key != null && _byKey.TryGetValue(key, out var existing)) return existing;

        var created = new GenericInstance(definition, arguments);
        _all.Add(created);
        if (key != null) _byKey.Add(key, created);
        return created;
    }

    private sealed class Key(IStruct definition, object[] parts) : IEquatable<Key>
    {
        public static Key? TryCreate(IStruct definition, ImmutableArray<IrExpression> arguments)
        {
            var parts = new object[arguments.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                object? part = Part(arguments[i]);

                if (part == null) return null;
                parts[i] = part;
            }

            return new Key(definition, parts);
        }

        private static object? Part(IrExpression argument) => argument switch
                {
                    IrComptimeInt integer => integer.BigInteger,
                    IrComptimeBool boolean => boolean.Value,
                    IrComptimeDecimal number => (object)("decimal", number.Value),
                    IrStringConstant text => text.Value,
                    IrReference { Reference: not UnsolvedRef } reference => reference.Reference,
                    IrSliceTypeModifier slice => Part(slice.subtype) is { } element ? ("slice", element) : null,
                    IrArrayTypeModifier { Length: IrComptimeInt length } array => Part(array.subtype) is { } element ? ("array", length.BigInteger, element) : null,
                    _ => null,
                };

        public bool Equals(Key? other) =>
            other != null && ReferenceEquals(definition, other.Definition) && parts.SequenceEqual(other.Parts);

        public override bool Equals(object? obj) => Equals(obj as Key);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(definition);
            foreach (var part in parts) hash.Add(part);
            return hash.ToHashCode();
        }

        private IStruct Definition => definition;
        private object[] Parts => parts;
    }
}
