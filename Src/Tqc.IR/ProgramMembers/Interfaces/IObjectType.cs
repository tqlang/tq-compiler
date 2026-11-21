namespace Tqc.IR.ProgramMembers;

/// <summary>
/// A struct, class, interface or typedef, whatever it is written in: what the object model needs to know about it.
/// Tq says it with attributes (`@abstract`, `protocol`, `@final`, `@class`); .NET says it in its metadata.
/// Everything that needs to know asks here, never the concrete type.
/// </summary>
public interface IObjectType : IMemberContainer
{
    /// <summary>It cannot be created: `@abstract` structs, protocols, abstract classes and interfaces of .NET.</summary>
    bool IsAbstract { get; }

    bool IsInterface { get; }

    /// <summary>It cannot be extended.</summary>
    bool IsFinal { get; }

    /// <summary>It is handled by reference itself, not only because of what it extends.</summary>
    bool IsReference { get; }
}
