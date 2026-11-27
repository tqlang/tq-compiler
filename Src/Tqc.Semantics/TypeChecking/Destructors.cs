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
                        case TqStruct { Destructor: not null }: return true;
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
