using System.Collections.Immutable;
using Tqc.IR.ProgramMembers;
using Tqc.IR.ProgramMembers.Primitive;
using Tqc.IR.References;

namespace Tqc.Dotnet.ProgramMembers;

/// <summary>
/// What a type of .NET has: its own members and the ones it inherits. A constructor is the type's own; the overloads of a
/// method that the type declares are joined with the ones it inherits under the same name (as `x.f(...)` does in C#,
/// except that a base overload with the same parameters is the one the type declares).
/// </summary>
internal static class DotnetInheritance
{
    public static IMember? Find(IMemberContainer self, ImmutableDictionary<string, IMember>? own, string name, IMemberContainer? fallback = null)
    {
        var declared = own?.GetValueOrDefault(name);
        if (name == ".ctor") return declared;

        var inherited = (self.Base ?? fallback)?.TryGetMember(name);
        if (declared == null) return inherited;

        return declared is DotnetMethodGroup ownGroup && inherited is DotnetMethodGroup baseGroup
            ? Merge(ownGroup, baseGroup)
            : declared;
    }

    /// <summary>`System.Object` as the container of what every value has, for the types that do not extend it in the IR.</summary>
    public static IMemberContainer? ObjectOf(ITypeRef? type)
    {
        // The type of a class is `Ref(Class)`: what it has is what the class has.
        if (CompilerStd.Types.IsRef(type, out var referred)) type = referred;
        return type is StructTypeReference { Struct: IMemberContainer container } ? container : null;
    }

    private static DotnetMethodGroup Merge(DotnetMethodGroup own, DotnetMethodGroup inherited)
    {
        var inheritedOnly = inherited.Methods.Where(method => !own.Methods.Any(declared =>
            declared.IsStatic == method.IsStatic && declared.ParameterTypes.SequenceEqual(method.ParameterTypes)));

        return new DotnetMethodGroup(own.Name) { Methods = [..own.Methods, ..inheritedOnly] };
    }
}
