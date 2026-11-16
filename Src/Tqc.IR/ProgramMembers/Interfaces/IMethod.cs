namespace Tqc.IR.ProgramMembers;

/// <summary>A function that is a member of a type, whatever it is written in.</summary>
public interface IMethod : IFunction
{
    bool IsStatic { get; }
    bool IsConstructor { get; }

    /// <summary>It has no implementation, so whatever extends its type has to provide one.</summary>
    bool IsAbstract { get; }

    /// <summary>A function of the same signature in a type that extends this one can replace it.</summary>
    bool CanBeOverridden { get; }
}
