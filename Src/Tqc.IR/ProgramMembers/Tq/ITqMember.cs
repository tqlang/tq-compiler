namespace Tqc.IR.ProgramMembers.Tq;

/// <summary>A member written in Tq: it has attributes and, once its namespace is known, an access rule.</summary>
public interface ITqMember : IMember
{
    /// <summary>As written; the binder replaces them with the ones whose arguments are bound.</summary>
    IReadOnlyList<TqAttribute> Attributes { get; set; }

    /// <summary>Set after lowering, when the namespace of the member is known.</summary>
    MemberAccess Access { get; set; }
}
