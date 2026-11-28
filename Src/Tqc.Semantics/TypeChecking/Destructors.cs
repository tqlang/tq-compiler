using Mono.Cecil;
using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>
/// Which types have a destructor: a struct or class of Tq that declares one (or extends one that does), and a type of .NET
/// that implements `IDisposable`, whose `Dispose` is its destructor.
/// </summary>
internal static class Destructors
{
    public static bool Has(ITypeRef type)
    {
        // `*T` is the reference to an object: only a class has one that can be disposed.
        var reference = CompilerStd.Types.IsRef(type, out var referred);
        if (reference)
        {
            type = referred;
            if (!IsClass(type)) return false;
        }

        object? definition = type switch
        {
            StructTypeReference { Struct: GenericInstance { Definition: var generic } } => generic,
            StructTypeReference { Struct: var structure } => structure,
            _ => null,
        };

        switch (definition)
        {
            case TqStruct tq:
            {
                IStruct? current = tq;
                var steps = 0;
                while (current != null && steps++ < 256)
                {
                    switch (current)
                    {
                        case TqStruct declaring when Analyze(declaring).Count >= 0 && declaring.DeclaresDestructor: return true;
                        case TqStruct next: current = next.BaseStruct; continue;
                        case DotnetClass dotnet: return IsDisposable(dotnet.TypeReference);
                        default: return false;
                    }
                }

                return false;
            }

            case DotnetClass dotnetClass: return IsDisposable(dotnetClass.TypeReference);
            case DotnetStruct dotnetStruct: return IsDisposable(dotnetStruct.TypeReference);
            default: return false;
        }
    }

    /// <summary>A value that is not a reference: a struct by value, or a type of .NET that is not a class.</summary>
    public static bool IsByValue(ITypeRef type) => !CompilerStd.Types.IsRef(type, out _) && !IsClass(type);

    /// <summary>
    /// Finds the fields of a struct that are destroyed with it (`TqStruct.DestroyedFields`): the ones, not static, that are by value and have a
    /// destructor. A reference is not: what it refers to may have other owners.
    /// </summary>
    public static IReadOnlyList<TqField> Analyze(TqStruct structure)
    {
        if (structure.DestroyedFields != null) return structure.DestroyedFields;

        // A struct by value cannot contain itself; this is only for the one that is being analyzed.
        structure.DestroyedFields = [];

        var fields = new List<TqField>();
        foreach (var field in structure.Members.Values.OfType<TqField>().OrderBy(field => field.SourceLocation.span.LineStart).ThenBy(field => field.SourceLocation.span.ColStart))
        {
            if (field.Type == null || field.Attributes.OfType<TqBuiltinAttribute>().Any(attribute => attribute.Kind == BuiltinAttribute.Static)) continue;
            if (TypeChecker.TypeFromExpression(field.Type) is { } type && IsByValue(type) && Has(type)) fields.Add(field);
        }

        structure.DestroyedFields = fields;
        return fields;
    }

    private static bool IsClass(ITypeRef type) => type switch
    {
        StructTypeReference { Struct: GenericInstance { Definition: var generic } } => IsClass(generic, 0),
        StructTypeReference { Struct: var structure } => IsClass(structure, 0),
        _ => false,
    };

    private static bool IsClass(IStruct structure, int steps) =>
        steps < 256 && structure is IObjectType type && (type.IsReference || (type.Base is IStruct parent && IsClass(parent, steps + 1)));

    private static bool IsDisposable(TypeReference type)
    {
        try
        {
            for (var current = type.Resolve(); current != null; current = current.BaseType?.Resolve())
            {
                if (current.Interfaces.Any(@interface => @interface.InterfaceType.FullName == "System.IDisposable")) return true;
            }
        }
        catch (AssemblyResolutionException)
        {
        }

        return false;
    }
}
