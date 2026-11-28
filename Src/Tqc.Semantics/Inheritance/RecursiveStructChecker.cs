using Tqc.Core;
using Tqc.Diagnostics;
using Tqc.IR.Nodes;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// A struct by value holds its fields inside itself, so it cannot hold itself, directly or through other structs by value:
/// it would have no size (.NET fails to load it). What is held by reference (`@class`, arrays, slices) does not count; what
/// wraps a value does (`?T`, `!T`, the struct that is extended, a generic struct with a field of its parameter).
/// </summary>
internal sealed class RecursiveStructChecker(DiagnosticBag diagnostics, Func<IrExpression, ITypeRef?> typeOf)
{
    /// <summary>A struct by value inside another one, with the structs given to its parameters (null for anything else).</summary>
    private sealed record Held(TqStruct Definition, Held?[] Arguments)
    {
        public bool Equals(Held? other) =>
            other != null && ReferenceEquals(Definition, other.Definition) && Arguments.SequenceEqual(other.Arguments);

        public override int GetHashCode() =>
            Arguments.Aggregate(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Definition),
                (hash, argument) => HashCode.Combine(hash, argument));
    }

    private sealed record Step(Held Struct, string Member, SourceLocation Location);

    /// <summary>How many times a generic struct can be inside itself, each time with other arguments, before it never ends.</summary>
    private const int MaximumNesting = 32;

    private readonly HashSet<Held> _done = [];
    private readonly HashSet<TqStruct> _reported = new(ReferenceEqualityComparer.Instance);
    private readonly List<Step> _path = [];

    /// <param name="typeOf">The type the checker gave to an expression: the one of a field declared without it.</param>
    public static void Check(TqNamespace root, DiagnosticBag diagnostics, Func<IrExpression, ITypeRef?> typeOf) =>
        new RecursiveStructChecker(diagnostics, typeOf).CheckNamespace(root);

    private void CheckNamespace(TqNamespace namespaceNode)
    {
        foreach (var fileScope in namespaceNode.FileScopes)
        foreach (var member in fileScope.Members.Values)
            CheckMember(member);

        foreach (var child in namespaceNode.Namespaces.Values) CheckNamespace(child);
    }

    private void CheckMember(IMember member)
    {
        switch (member)
        {
            case TqStruct structure:
                if (IsByValue(structure)) Visit(new Held(structure, new Held?[structure.Parameters.Count]));
                foreach (var inner in structure.Members.Values) CheckMember(inner);
                break;

            case TqTypedef typedef:
                foreach (var inner in typedef.Members.Values) CheckMember(inner);
                break;
        }
    }

    private static bool IsByValue(TqStruct structure) => !structure.IsInterface && !StructSemantics.IsReference(structure);

    private void Visit(Held held)
    {
        if (_done.Contains(held)) return;

        foreach (var (name, type, location) in StorageOf(held.Definition))
        {
            var inside = Resolve(type, held);
            if (inside == null) continue;

            _path.Add(new Step(held, name, location));

            var start = _path.FindIndex(step => step.Struct.Equals(inside));
            if (start >= 0) Report(start);
            else if (_path.Count(step => ReferenceEquals(step.Struct.Definition, inside.Definition)) >= MaximumNesting)
                Report(_path.FindIndex(step => ReferenceEquals(step.Struct.Definition, inside.Definition)));
            else Visit(inside);

            _path.RemoveAt(_path.Count - 1);
        }

        _done.Add(held);
    }

    /// <summary>The cycle that begins at `start` of the path, reported once, where its first member is declared.</summary>
    private void Report(int start)
    {
        var cycle = _path.Skip(start).ToList();
        if (cycle.Any(step => _reported.Contains(step.Struct.Definition))) return;
        foreach (var step in cycle) _reported.Add(step.Struct.Definition);

        var first = cycle[0];
        var through = string.Join(" -> ", cycle.Take(MaximumNesting / 8).Select(step => $"{step.Struct.Definition.Name}.{step.Member}"));
        if (cycle.Count > MaximumNesting / 8) through += " -> ...";

        diagnostics.ReportError(new TypeError(
            $"The struct '{first.Struct.Definition.Name}' is by value and contains itself, through {through}. "
            + "To break the cycle, make one of these structs '@class' and refer to it with '*'",
            first.Location));
    }

    /// <summary>What takes room in a value of the struct: what it extends, its fields and the storage of its properties.</summary>
    private IEnumerable<(string Name, ITypeRef? Type, SourceLocation Location)> StorageOf(TqStruct structure)
    {
        if (structure.Extends != null)
            yield return ("extends", TypeExpressions.Raw(structure.Extends), structure.SourceLocation);

        foreach (var member in structure.Members.Values)
        {
            switch (member)
            {
                // A constant and a `@static` field are not inside the values.
                case TqField field when !IsStatic(field) && !IsConstant(field.Type):
                    var type = field.Type != null ? TypeExpressions.Raw(field.Type)
                        : field.Value != null ? typeOf(field.Value) : null;
                    yield return (field.Name, type, field.SourceLocation);
                    break;

                case TqProperty { HasBackingField: true, IsStatic: false } property:
                    var expression = property.Type ?? property.GetterFunction?.ReturnType;
                    if (expression != null) yield return (property.Name, TypeExpressions.Raw(expression), property.SourceLocation);
                    break;
            }
        }
    }

    private static bool IsStatic(TqField field) =>
        field.Attributes.OfType<TqBuiltinAttribute>().Any(attribute => attribute.Kind == BuiltinAttribute.Static);

    private static bool IsConstant(IrExpression? type) =>
        type != null && TypeExpressions.Raw(type) is StructTypeReference { Struct: GenericInstance instance }
                     && ReferenceEquals(instance.Definition, CompilerStd.Types.Const);

    /// <summary>The struct by value that a value of this type holds, if any, seen from inside `holder`.</summary>
    private static Held? Resolve(ITypeRef? type, Held holder)
    {
        var std = CompilerStd.Types;

        switch (type)
        {
            case GenericParameterTypeRef generic:
                for (var i = 0; i < holder.Definition.Parameters.Count; i++)
                    if (ReferenceEquals(holder.Definition.Parameters[i], generic.Parameter)) return holder.Arguments[i];
                return null;

            case StructTypeReference { Struct: GenericInstance instance }:
                if (ReferenceEquals(instance.Definition, std.Const) || ReferenceEquals(instance.Definition, std.Nullable)
                                                                     || ReferenceEquals(instance.Definition, std.Failable))
                    return instance.Arguments is [var inner] ? Resolve(TypeExpressions.Raw(inner), holder) : null;

                return instance.Definition is TqStruct generated && IsByValue(generated)
                                                                 && instance.Arguments.Length == generated.Parameters.Count
                    ? new Held(generated, [..instance.Arguments.Select(argument => Resolve(TypeExpressions.Raw(argument), holder))])
                    : null;

            case StructTypeReference { Struct: TqStruct structure } when IsByValue(structure):
                return new Held(structure, new Held?[structure.Parameters.Count]);

            default:
                return null;
        }
    }
}
