using Tqc.Dotnet.ProgramMembers;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.ProgramMembers.Tq;
using Tqc.IR.References;

namespace Tqc.Semantics;

/// <summary>Whether a struct is handled by value or by reference.</summary>
internal static class StructSemantics
{
    /// <summary>
    /// By reference: what says so itself (.NET classes, structs of Tq marked `@class`) and whatever extends one of those.
    /// Everything else is by value, also when it extends another struct.
    /// </summary>
    public static bool IsReference(IStruct structure) => IsReference(structure, []);

    private static bool IsReference(IStruct structure, HashSet<IStruct> visited)
    {
        if (!visited.Add(structure)) return false;

        return structure switch
        {
            GenericInstance instance => IsReference(instance.Definition, visited),
            IObjectType type => type.IsReference || (type.Base is IStruct baseStruct && IsReference(baseStruct, visited)),
            _ => false,
        };
    }

    /// <summary>The type of a value of the struct: a reference when the struct is one.</summary>
    public static ITypeRef TypeOf(IStruct structure)
    {
        var type = new StructTypeReference(structure);
        return IsReference(structure) ? CompilerStd.Types.RefOf(type) : type;
    }

    /// <summary>The type of a value whose type is written as `written`: a struct that is by reference is `Ref(it)`.</summary>
    public static ITypeRef ValueTypeOf(ITypeRef written) =>
        written is StructTypeReference { Struct: var structure } ? TypeOf(structure) : written;
}
