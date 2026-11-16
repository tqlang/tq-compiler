namespace Tqc.IR.ProgramMembers;

/// <summary>
/// A member that exposes children by name. Namespaces do not need it: INamespace has its own TryGetMember.
/// </summary>
public interface IMemberContainer
{
    IMember? TryGetMember(string memberName);

    /// <summary>What it extends, whatever language that is written in; null if it extends nothing known.</summary>
    IMemberContainer? Base => null;

    /// <summary>The members it declares itself, not the ones it inherits.</summary>
    IEnumerable<IMember> OwnMembers => [];
}
